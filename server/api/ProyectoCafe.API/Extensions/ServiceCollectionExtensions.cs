using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using ProyectoCafe.Application.Extensions;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Infrastructure.Extensions;

namespace ProyectoCafe.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProyectoCafeApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();

        services.AddCors(options =>
        {
            options.AddPolicy("Portal", policy =>
            {
                var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    // Credentials are NOT compatible with AllowAnyOrigin. Force explicit configuration.
                    throw new InvalidOperationException("Cors:AllowedOrigins debe configurarse cuando se usa auth por cookies.");
                }
            });
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProyectoCafe API", Version = "v1" });

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header usando el esquema Bearer. Ej: \"Bearer {token}\""
            };
            c.AddSecurityDefinition("Bearer", scheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                }
            });
        });

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection("Auth"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();

        var auth = configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();
        var issuer = auth.Jwt.Issuer;
        var audience = auth.Jwt.Audience;
        var signingKey = auth.Jwt.SigningKey;
        var accessTokenCookieName = auth.Cookie.AccessTokenCookieName;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        // Read JWT from HttpOnly cookie.
                        if (ctx.Request.Cookies.TryGetValue(accessTokenCookieName, out var token) &&
                            !string.IsNullOrWhiteSpace(token))
                        {
                            ctx.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async ctx =>
                    {
                        var uid = ctx.Principal?.FindFirst("uid")?.Value;
                        if (!int.TryParse(uid, out var userId))
                        {
                            ctx.Fail("Missing uid");
                            return;
                        }

                        var db = ctx.HttpContext.RequestServices.GetRequiredService<ProyectoCafeDbContext>();
                        var isActive = await db.Usuarios
                            .AnyAsync(u => u.UsuarioId == userId && u.Estado == UsuarioEstado.Activo, ctx.HttpContext.RequestAborted);

                        if (!isActive)
                        {
                            ctx.Fail("User inactive");
                        }
                    },
                };
            });

        services.AddHttpContextAccessor();

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<PermissionCatalogService>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ILoginRateLimiter, LoginRateLimiter>();

        services.AddHostedService<SecurityBootstrapHostedService>();

        services.AddApplication();
        services.AddInfrastructure(configuration);

        return services;
    }
}
