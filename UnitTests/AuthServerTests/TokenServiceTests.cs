using System.Security.Claims;
using System.Security.Cryptography;
using AuthServer.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace UnitTests.AuthServer;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ClavePrivada"] = privateKey,
                ["Jwt:MinutosExpiracion"] = "60",
                ["Jwt:Issuer"] = "https://auth.example.com",
                ["Jwt:Audience"] = "https://api.example.com",
                ["Clientes:TestClient"] = "TestSecret"
            })
            .Build();

        _tokenService = new TokenService(configuration);
    }

    [Fact]
    public void ValidarCliente_ConCredencialesCorrectas_DevuelveTrue()
    {
        // Arrange: preparar las credenciales válidas del cliente registrado.
        var clientId = "TestClient";
        var secret = "TestSecret";

        // Act: validar las credenciales con TokenService.
        var result = _tokenService.ValidarCliente(clientId, secret);

        // Assert: confirmar que el cliente queda autenticado.
        Assert.True(result);
    }

    [Theory]
    [InlineData("TestClient", "SecretIncorrecto")]
    [InlineData("ClienteInexistente", "TestSecret")]
    [InlineData("TestClient", "")]
    public void ValidarCliente_ConCredencialesIncorrectas_DevuelveFalse(string clientId, string secret)
    {
        // Arrange: usar las credenciales inválidas proporcionadas por cada caso parametrizado.

        // Act: validar las credenciales recibidas.
        var result = _tokenService.ValidarCliente(clientId, secret);

        // Assert: confirmar que la validación las rechaza.
        Assert.False(result);
    }

    [Fact]
    public void CrearToken_IncluyeSubjectClaimsYExpiracion()
    {
        // Arrange: definir el subject y el claim adicional del JWT.
        var subject = "usuario-1";
        var claims = new[] { new Claim("email", "test@example.com") };

        // Act: generar el token con el subject y el claim preparados.
        var result = _tokenService.CrearToken(subject, claims);

        // Assert: verificar el token, su expiración y los claims serializados.
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(3600, result.ExpiresIn);

        using var payload = System.Text.Json.JsonDocument.Parse(
            Base64UrlDecode(result.AccessToken.Split('.')[1]));
        Assert.Equal("usuario-1", payload.RootElement.GetProperty("sub").GetString());
        Assert.Equal("test@example.com", payload.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public void CrearToken_SinClaimsAdicionales_GeneraToken()
    {
        // Arrange: definir un subject y una colección vacía de claims adicionales.
        var subject = "usuario-2";
        var claims = Array.Empty<Claim>();

        // Act: generar el JWT sin claims adicionales.
        var result = _tokenService.CrearToken(subject, claims);

        // Assert: comprobar que el token se generó y conserva la expiración configurada.
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(3600, result.ExpiresIn);
    }

    private static byte[] Base64UrlDecode(string value)
    {
        value = value.Replace('-', '+').Replace('_', '/');
        value = value.PadRight(value.Length + (4 - value.Length % 4) % 4, '=');
        return Convert.FromBase64String(value);
    }
}
