using System.Security.Claims;
using Base.Core;
using Base.EventBus.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Base.AspNetCore;

public class HttpContextTraceAccessor : ITraceAccesor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTraceAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
        return _httpContextAccessor.HttpContext?.GetCorrelationId() ?? Guid.NewGuid().ToString("N");
    }

    public string GetUserId()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        Check.NotNull(principal, nameof(principal));

        var userIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdOrNull == null || userIdOrNull.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        return userIdOrNull.Value;
    }

    public string[] GetRoles()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        Check.NotNull(principal, nameof(principal));

        var roles = principal?.Claims.Where(c => c.Type == ClaimTypes.Role).ToArray() ?? Array.Empty<Claim>();

        return roles.Select(c => c.Value).Distinct().ToArray();
    }
}