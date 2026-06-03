using StampService.API.Middlewares;
using StampService.Application.Audit;

namespace StampService.API.Audit;

public sealed class HttpBusinessAuditContext : IBusinessAuditContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpBusinessAuditContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Channel => "Web";

    public string? TraceId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
                return null;

            return httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var correlationId)
                ? correlationId as string
                : httpContext.TraceIdentifier;
        }
    }
}
