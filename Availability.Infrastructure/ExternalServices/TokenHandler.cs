using System.Net.Http.Headers;
using Availability.Application.Interfaces;

namespace Availability.Infrastructure.ExternalServices;

// Intermediario del HttpClient de Booking: antes de cada pedido agrega "Authorization: Bearer <token>".
// Así BookingHttpClient no tiene que ocuparse del token.
public class TokenHandler : DelegatingHandler
{
    private readonly IAuthServerService _authServer;

    public TokenHandler(IAuthServerService authServer)
    {
        _authServer = authServer;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authServer.ObtenerTokenServicioAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}