using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Availability.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Availability.Infrastructure.ExternalServices;

// Adaptador del AuthServer: pide los tokens por HTTP a POST /connect/token.
public class AuthServerHttpClient : IAuthServerService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private string? _token;
    private DateTime _vence;

    public AuthServerHttpClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    // Token de servicio (Client Credentials). Se reutiliza hasta que esté por vencer.
    public async Task<string> ObtenerTokenServicioAsync()
    {
        if (_token != null && DateTime.UtcNow < _vence)
            return _token;

        var client = _httpClientFactory.CreateClient();
        var respuesta = await client.PostAsync(_configuration["AuthServer:Url"] + "connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _configuration["AuthServer:ClientId"]!,
                ["client_secret"] = _configuration["AuthServer:ClientSecret"]!
            }));
        respuesta.EnsureSuccessStatusCode();

        var datos = await respuesta.Content.ReadFromJsonAsync<RespuestaToken>();
        _token = datos!.AccessToken;
        _vence = DateTime.UtcNow.AddSeconds(datos.ExpiresIn - 60); // se pide uno nuevo 1 minuto antes de que venza
        return _token;
    }

    // Login de usuario: cambia el id_token de Google por el token del sistema (grant_type=google).
    public async Task<string?> ObtenerTokenUsuarioAsync(string idTokenGoogle)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var respuesta = await client.PostAsync(_configuration["AuthServer:Url"] + "connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "google",
                    ["id_token"] = idTokenGoogle
                }));
            if (!respuesta.IsSuccessStatusCode)
                return null;

            var datos = await respuesta.Content.ReadFromJsonAsync<RespuestaToken>();
            return datos!.AccessToken;
        }
        catch (HttpRequestException)
        {
            return null; // el AuthServer no respondió
        }
    }

    private class RespuestaToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}