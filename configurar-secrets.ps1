<#
.SYNOPSIS
    Carga los User Secrets de AuthServer, AvailabilitySite y Booking de una sola vez.

.DESCRIPTION
    Pide los valores por consola y los guarda con "dotnet user-secrets set"
    en cada proyecto que los necesita. Si existe secrets.json (no se commitea),
    toma de ahí los valores que tenga y solo pregunta los que falten.

    Los valores compartidos se cargan en todos los proyectos que los usan, así
    quedan iguales en ambos lados:
      - El secreto de cliente de Booking va a AuthServer (Clientes:booking-business)
        y a Booking (AuthServer:ClientSecret).
      - El de AvailabilitySite va a AuthServer (Clientes:availability-site)
        y a AvailabilitySite (AuthServer:ClientSecret).
      - El connection string de Service Bus va a AvailabilitySite y a Booking.

    Si ClientSecretBooking o ClientSecretAvailabilitySite quedan vacíos (Enter
    al preguntar), se generan al azar: solo tienen que coincidir entre
    AuthServer y el cliente, y el script ya los carga iguales en ambos.

.EXAMPLE
    .\configurar-secrets.ps1
    .\configurar-secrets.ps1 -Archivo C:\ruta\a\mis-secrets.json
#>
param(
    [string]$Archivo = (Join-Path $PSScriptRoot 'secrets.json')
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'No se encontró "dotnet" en el PATH. Instalá el SDK de .NET y volvé a correr el script.'
}

$valores = $null
if (Test-Path $Archivo) {
    Write-Host "Usando los valores de $Archivo (los que falten se preguntan)." -ForegroundColor Cyan
    $valores = Get-Content $Archivo -Raw -Encoding UTF8 | ConvertFrom-Json
}

function NuevoSecreto {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return [Convert]::ToBase64String($bytes)
}

# Toma el valor de secrets.json y, si no está, lo pide por consola.
# Los obligatorios se vuelven a pedir hasta que no queden vacíos.
function Obtener([string]$nombre, [string]$descripcion, [bool]$obligatorio = $true) {
    if ($null -ne $valores -and $null -ne $valores.$nombre) {
        $v = ([string]$valores.$nombre).Trim()
        if ($v) { return $v }
    }

    Write-Host ''
    Write-Host "$nombre" -ForegroundColor Yellow -NoNewline
    Write-Host " - $descripcion"
    while ($true) {
        $v = ([string](Read-Host '  Pegá el valor')).Trim()
        if ($v -or -not $obligatorio) { return $v }
        Write-Host '  Este valor es obligatorio.' -ForegroundColor Red
    }
}

$jwtClavePrivada = Obtener 'JwtClavePrivada'            'clave privada RSA del AuthServer, en base64 (pareja de la ClavePublica de los appsettings)'
$serviceBus      = Obtener 'ServiceBusConnectionString' 'connection string de Azure Service Bus (Endpoint=sb://...)'
$googleSecret    = Obtener 'GoogleClientSecret'         'client secret de Google para el login de AvailabilitySite'
$secretoBooking  = Obtener 'ClientSecretBooking'          'secreto de cliente de Booking (Enter para generar uno)' $false
$secretoSite     = Obtener 'ClientSecretAvailabilitySite' 'secreto de cliente de AvailabilitySite (Enter para generar uno)' $false

if (-not $secretoBooking) { $secretoBooking = NuevoSecreto; Write-Host 'ClientSecretBooking: se generó uno nuevo.' }
if (-not $secretoSite)    { $secretoSite    = NuevoSecreto; Write-Host 'ClientSecretAvailabilitySite: se generó uno nuevo.' }

# Proyecto -> secretos que necesita
$secretos = [ordered]@{
    'AuthServer' = [ordered]@{
        'Jwt:ClavePrivada'            = $jwtClavePrivada
        'Clientes:booking-business'   = $secretoBooking
        'Clientes:availability-site'  = $secretoSite
    }
    'AvailabilitySite' = [ordered]@{
        'Google:ClientSecret'         = $googleSecret
        'AuthServer:ClientSecret'     = $secretoSite
        'ServiceBus:ConnectionString' = $serviceBus
    }
    'Booking' = [ordered]@{
        'ConnectionStrings:AzureServiceBus' = $serviceBus
        'AuthServer:ClientSecret'           = $secretoBooking
    }
}

$errores = 0
foreach ($proyecto in $secretos.Keys) {
    $csproj = Join-Path $PSScriptRoot "$proyecto\$proyecto.csproj"
    Write-Host ''
    Write-Host "== $proyecto" -ForegroundColor Cyan

    foreach ($clave in $secretos[$proyecto].Keys) {
        $valor = $secretos[$proyecto][$clave]
        $salida = & dotnet user-secrets set --project $csproj $clave $valor 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   OK  $clave" -ForegroundColor Green
        } else {
            Write-Host "   ERR $clave" -ForegroundColor Red
            Write-Host "       $salida"
            $errores++
        }
    }
}

Write-Host ''
if ($errores -gt 0) {
    Write-Host "Terminó con $errores error(es)." -ForegroundColor Red
    exit 1
}
Write-Host 'Listo: secretos cargados en AuthServer, AvailabilitySite y Booking.' -ForegroundColor Green
Write-Host 'Payment.Grpc no necesita secretos (solo usa la clave pública, que está en appsettings.json).'
