### Prompt 1

Estoy en el TP de reservas con microservicios en .NET y me toca Booking. Tiene que ser N-Layer con dominio anémico y con su propia base de datos. Explicame por qué hacés cada paso. Las reservas tienen que aguantar varias réplicas y mucha gente pidiendo el mismo asiento al mismo tiempo, sin que se reserve dos veces

### Prompt 2

OK a la etapa de dominio y datos

### Prompt 3

El grupo usa net9. Revisá Domain y Data: pasalos a net9 con EF Core 9, que la base use SQL Server LocalDB con la connection string fuera del repo, creá la base y verificá que compile

### Prompt 4

Armá una branch nueva desde main con solo Domain, Data, la configuración comun, el .slnx y este archivo. Usá un worktree para no tocar mi otra branch. Para verificar las migraciones, borra la base local y volvé a crearla desde cero

### Prompt 5

OK a instalar dotnet-ef 9 y a recrear la base. Mostrame las tablas, los índices y el historial de migraciones

### Prompt 6

Mostrame IReservaRepository con una explicación de cada método y sus excepciones, para pasársela a mi compañero que hace Business. Corregí el comentario de VencerRetenidas

### Prompt 7

Me adapto a la estructura del grupo: Booking es un solo proyecto con carpetas, y Availability usa SQLite. Creá la branch booking/datos-sqlite desde main. Pasá mi Domain a Booking.Entities y mi Data a Booking.Data, con la convención de mi compañero y usando su ReservationStatusEnum, sin tocar sus archivos sin preguntarme. Pasá la base a SQLite (índices únicos, token de concurrencia, traducción de errores, VencerRetenidas atómico), hacé una sola migración inicial y que booking.db no se suba

### Prompt 8

OK a los namespaces (raíz + carpeta), al token Version y a usar su enum. El arreglo del .slnx va en un commit aparte. La connection string va en el appsettings de Bruno, con un valor por defecto, porque ese appsettings no se carga

### Prompt 9

OK al cambio del .csproj. Compilá la solución, generá la migración, creá la base, mostrame tablas e índices y verificá que booking.db no se suba

### Prompt 10

Antes de subir, probá ReservaRepository a través de EF con un programa temporal fuera del repo: alta, asiento ocupado, solicitud duplicada, conflicto de Version, asientos ocupados y vencimiento