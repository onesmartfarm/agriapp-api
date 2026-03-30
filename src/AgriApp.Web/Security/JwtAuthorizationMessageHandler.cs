using System.Net.Http.Headers;

namespace AgriApp.Web.Security;

public class JwtAuthorizationMessageHandler : DelegatingHandler
{
    private readonly JwtAuthenticationStateProvider _authProvider;

    public JwtAuthorizationMessageHandler(JwtAuthenticationStateProvider authProvider)
    {
        _authProvider = authProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _authProvider.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
