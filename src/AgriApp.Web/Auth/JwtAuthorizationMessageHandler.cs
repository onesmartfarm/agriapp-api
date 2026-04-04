using Blazored.LocalStorage;

namespace AgriApp.Web.Auth;

public class JwtAuthorizationMessageHandler : DelegatingHandler
{
    private const string TokenKey = "agriapp_token";

    private readonly ILocalStorageService _localStorage;

    public JwtAuthorizationMessageHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsStringAsync(TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
