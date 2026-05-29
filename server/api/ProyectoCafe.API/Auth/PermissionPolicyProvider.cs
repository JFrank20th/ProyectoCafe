using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ProyectoCafe.API.Auth;

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new(StringComparer.OrdinalIgnoreCase);

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Permissions.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        if (_cache.TryGetValue(policyName, out var cached))
        {
            return Task.FromResult<AuthorizationPolicy?>(cached);
        }

        var payload = policyName[Permissions.PolicyPrefix.Length..];
        var parts = payload.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        var modulo = parts[0];
        var accion = parts[1];

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(modulo, accion))
            .Build();

        _cache.TryAdd(policyName, policy);
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
