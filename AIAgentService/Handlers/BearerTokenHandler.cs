using System.Net.Http.Headers;
using Shared.Events.Extensions;

namespace AIAgentService.Handlers;

public class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Check if user request has a Bearer token to forward
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            // 2. Generate System JWT Token for AIAgentService inter-service communication
            var systemToken = JwtExtensions.GenerateSystemToken(_configuration, "AIAgentService", "Admin");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", systemToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
