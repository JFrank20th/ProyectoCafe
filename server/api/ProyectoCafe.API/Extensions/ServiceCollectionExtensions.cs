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
using ProyectoCafe.API.Configuration;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Infrastructure.Extensions;

namespace ProyectoCafe.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProyectoCafeApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();

        // La API es la única autoridad CORS. El Portal no debe emitir cabeceras
        // Access-Control-*: si lo hiciera, se duplicarían o contradirían estas.
        // La lista se resuelve al construir la política (después de que todos los
        // proveedores de configuración estén cargados) y se valida al arrancar en
        // Program.cs para fallar rápido ante una configuración inválida.
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyNames.Portal, policy =>
            {
                var allowedOrigins = CorsOrigins.Resolve(configuration);

                // WithOrigins + AllowCredentials: Access-Control-Allow-Origin recibe
                // el origen exacto autorizado, nunca "*".
                policy.WithOrigins(allowedOrigins)
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProyectoCafe API", Version = "v1" });

            var scheme = new OpenApiSecurityScheme
            {
                Name = "pc_at",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Description = "Cookie HttpOnly emitida por POST /api/v1/auth/login. El JWT no se acepta por cabecera Authorization."
            };
            c.AddSecurityDefinition("CookieAuth", scheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "CookieAuth" } },
                    Array.Empty<string>()
                }
            });
        });

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection("Auth"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>>((options, authOptions) =>
            {
                var auth = authOptions.Value;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = auth.Jwt.Issuer,
                    ValidAudience = auth.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.Jwt.SigningKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        // La única fuente aceptada del JWT es la cookie HttpOnly.
                        // No se permite el fallback a la cabecera Authorization.
                        if (ctx.Request.Cookies.TryGetValue(auth.Cookie.AccessTokenCookieName, out var token) &&
                            !string.IsNullOrWhiteSpace(token))
                        {
                            ctx.Token = token;
                        }
                        else
                        {
                            ctx.NoResult();
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
