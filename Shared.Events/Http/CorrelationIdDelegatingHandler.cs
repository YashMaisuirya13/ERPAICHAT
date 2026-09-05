using Microsoft.AspNetCore.Http;

namespace Shared.Events.Http;

public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            var correlationId = _httpContextAccessor.HttpContext.TraceIdentifier;
            if (!request.Headers.Contains(CorrelationIdHeaderName) && !string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Add(CorrelationIdHeaderName, correlationId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
