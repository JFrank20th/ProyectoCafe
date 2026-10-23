using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Abstractions.Security;

namespace ProyectoCafe.Tests;

public sealed class LoginRateLimiterTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Check_BlocksIpAfterMaxFailuresAcrossDifferentUsers()
    {
        var clock = new FakeTimeProvider(Start);
        var limiter = CreateLimiter(clock, new LoginRateLimitOptions
        {
            IpMaxFailedAttempts = 2,
            IpWindowMinutes = 5,
            UserMaxFailedAttempts = 100,
            UserWindowMinutes = 15,
        });

        limiter.RecordFailure("203.0.113.10", "user-a@test.local");
        limiter.RecordFailure("203.0.113.10", "user-b@test.local");

        var decision = limiter.Check("203.0.113.10", "user-c@test.local");

        Assert.False(decision.IsAllowed);
        Assert.Equal(LoginRateLimitScope.Ip, decision.Scope);
        Assert.Equal(2, decision.FailedAttempts);
        Assert.Equal(TimeSpan.FromMinutes(5), decision.RetryAfter);

        Assert.True(limiter.Check("203.0.113.11", "user-c@test.local").IsAllowed);
    }

    [Fact]
    public void Check_BlocksUserAfterMaxFailuresAcrossDifferentIps()
    {
        var clock = new FakeTimeProvider(Start);
        var limiter = CreateLimiter(clock, new LoginRateLimitOptions
        {
            IpMaxFailedAttempts = 100,
            IpWindowMinutes = 5,
            UserMaxFailedAttempts = 2,
            UserWindowMinutes = 15,
        });

        limiter.RecordFailure("203.0.113.10", "victim@test.local");
        limiter.RecordFailure("203.0.113.11", "VICTIM@test.local");

        var decision = limiter.Check("203.0.113.12", "victim@test.local");

        Assert.False(decision.IsAllowed);
        Assert.Equal(LoginRateLimitScope.User, decision.Scope);
        Assert.Equal(2, decision.FailedAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), decision.RetryAfter);
    }

    [Fact]
    public void Check_AllowsAgainAfterWindowExpires()
    {
        var clock = new FakeTimeProvider(Start);
        var limiter = CreateLimiter(clock, new LoginRateLimitOptions
        {
            IpMaxFailedAttempts = 1,
            IpWindowMinutes = 5,
            UserMaxFailedAttempts = 1,
            UserWindowMinutes = 5,
        });

        limiter.RecordFailure("203.0.113.10", "user@test.local");
        Assert.False(limiter.Check("203.0.113.10", "user@test.local").IsAllowed);

        clock.UtcNow = Start.AddMinutes(5).AddSeconds(1);

        Assert.True(limiter.Check("203.0.113.10", "user@test.local").IsAllowed);
    }

    [Fact]
    public void RecordSuccess_ClearsUserWindowButNotIpWindow()
    {
        var clock = new FakeTimeProvider(Start);
        var limiter = CreateLimiter(clock, new LoginRateLimitOptions
        {
            IpMaxFailedAttempts = 1,
            IpWindowMinutes = 5,
            UserMaxFailedAttempts = 1,
            UserWindowMinutes = 15,
        });

        limiter.RecordFailure("203.0.113.10", "user@test.local");
        limiter.RecordSuccess("203.0.113.10", "user@test.local");

        var sameUserOtherIp = limiter.Check("203.0.113.11", "user@test.local");
        Assert.True(sameUserOtherIp.IsAllowed);

        var otherUserSameIp = limiter.Check("203.0.113.10", "other@test.local");
        Assert.False(otherUserSameIp.IsAllowed);
        Assert.Equal(LoginRateLimitScope.Ip, otherUserSameIp.Scope);
    }

    [Fact]
    public void Check_WhenDisabled_AllowsEveryAttempt()
    {
        var clock = new FakeTimeProvider(Start);
        var limiter = CreateLimiter(clock, new LoginRateLimitOptions
        {
            Enabled = false,
            IpMaxFailedAttempts = 1,
            IpWindowMinutes = 5,
            UserMaxFailedAttempts = 1,
            UserWindowMinutes = 15,
        });

        limiter.RecordFailure("203.0.113.10", "user@test.local");

        Assert.True(limiter.Check("203.0.113.10", "user@test.local").IsAllowed);
    }

    private static LoginRateLimiter CreateLimiter(FakeTimeProvider clock, LoginRateLimitOptions rateLimit) =>
        new(
            Options.Create(new AuthOptions { RateLimit = rateLimit }),
            clock,
            NullLogger<LoginRateLimiter>.Instance);

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
