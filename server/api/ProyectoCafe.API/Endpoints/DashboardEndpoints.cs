using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.API.Endpoints;

/// <summary>
/// Resumen del Dashboard para los módulos vigentes (Administración).
/// Expone indicadores de usuarios, roles, permisos y cobertura de acceso.
/// </summary>
public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/dashboard");
        Map(endpoints, "/api/Dashboard"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Dashboard");

        group.MapGet("/summary", async (
            string? period,
            string? from,
            string? to,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            DashboardPeriodWindow window;
            var nowUtc = DateTime.UtcNow;

            try
            {
                window = DashboardPeriodWindow.Resolve(period, from, to, nowUtc);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(period)] = [ex.Message],
                });
            }

            var current = await BuildMetricsAsync(db, window, ct);

            DashboardPreviousPeriodSummary? previousPeriod = null;
            var previousWindow = window.CreatePreviousWindow();
            if (previousWindow is not null)
            {
                var previous = await BuildMetricsAsync(db, previousWindow, ct);
                previousPeriod = new DashboardPreviousPeriodSummary
                {
                    Window = previousWindow.ToResponse(),
                    Users = previous.Users,
                    Roles = previous.Roles,
                    Permissions = previous.Permissions,
                    Access = previous.Access,
                };
            }

            var response = new DashboardSummaryResponse
            {
                Window = window.ToResponse(),
                PreviousPeriod = previousPeriod,
                Users = current.Users,
                Roles = current.Roles,
                Permissions = current.Permissions,
                Access = current.Access,
                GeneratedAtUtc = nowUtc,
            };

            return Results.Ok(response);
        })
        .RequireAuthorization();
    }

    private static async Task<DashboardMetricSnapshot> BuildMetricsAsync(
        ProyectoCafeDbContext db,
        DashboardPeriodWindow window,
        CancellationToken ct)
    {
        var startUtc = window.StartInclusiveUtc;
        var endUtc = window.EndExclusiveUtc;

        // Usuarios vigentes (se excluyen los eliminados lógicamente).
        var usersQuery = db.Usuarios.Where(usuario => usuario.Estado != UsuarioEstado.Eliminado);

        var totalUsers = await usersQuery.CountAsync(ct);
        var activeUsers = await usersQuery
            .CountAsync(usuario => usuario.Estado == UsuarioEstado.Activo, ct);
        var pendingUsers = await usersQuery
            .CountAsync(usuario => usuario.Estado == UsuarioEstado.PendienteActivacion, ct);
        var inactiveUsers = await usersQuery
            .CountAsync(usuario => usuario.Estado == UsuarioEstado.Inactivo, ct);

        // Usuarios creados dentro de la ventana consultada.
        var newUsersQuery = usersQuery;
        if (startUtc.HasValue)
        {
            newUsersQuery = newUsersQuery.Where(usuario => usuario.CreadoEn >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            newUsersQuery = newUsersQuery.Where(usuario => usuario.CreadoEn < endUtc.Value);
        }

        var newUsers = await newUsersQuery.CountAsync(ct);

        // Roles vigentes (se excluyen los eliminados lógicamente).
        var rolesQuery = db.Roles.Where(rol => rol.Estado != RolEstado.Eliminado);
        var totalRoles = await rolesQuery.CountAsync(ct);
        var activeRoles = await rolesQuery
            .CountAsync(rol => rol.Estado == RolEstado.Activo, ct);

        var roleAssignments = await db.UsuariosRoles.CountAsync(ct);

        // Permisos efectivos registrados por rol.
        var permissionAssignments = await db.RolesPermisos.CountAsync(ct);
        var distinctPermissions = await db.RolesPermisos
            .Select(permiso => new { permiso.Modulo, permiso.Accion })
            .Distinct()
            .CountAsync(ct);
        var permissionModules = await db.RolesPermisos
            .Select(permiso => permiso.Modulo)
            .Distinct()
            .CountAsync(ct);

        // Usuarios activos que tienen al menos un rol vigente asignado.
        var usersWithRoles = await db.UsuariosRoles
            .Join(
                db.Usuarios.Where(usuario => usuario.Estado == UsuarioEstado.Activo),
                asignacion => asignacion.UsuarioId,
                usuario => usuario.UsuarioId,
                (asignacion, _) => asignacion)
            .Join(
                db.Roles.Where(rol => rol.Estado != RolEstado.Eliminado),
                asignacion => asignacion.RolId,
                rol => rol.RolId,
                (asignacion, _) => asignacion.UsuarioId)
            .Distinct()
            .CountAsync(ct);

        var coveragePercent = activeUsers == 0
            ? 0m
            : Math.Round(usersWithRoles * 100m / activeUsers, 1, MidpointRounding.AwayFromZero);

        return new DashboardMetricSnapshot
        {
            Users = new DashboardUsersSummary
            {
                TotalCount = totalUsers,
                ActiveCount = activeUsers,
                PendingCount = pendingUsers,
                InactiveCount = inactiveUsers,
                NewCount = newUsers,
            },
            Roles = new DashboardRolesSummary
            {
                TotalCount = totalRoles,
                ActiveCount = activeRoles,
                AssignmentsCount = roleAssignments,
            },
            Permissions = new DashboardPermissionsSummary
            {
                AssignmentsCount = permissionAssignments,
                DistinctCount = distinctPermissions,
                ModuleCount = permissionModules,
            },
            Access = new DashboardAccessSummary
            {
                ActiveUsersCount = activeUsers,
                UsersWithRolesCount = usersWithRoles,
                CoveragePercent = coveragePercent,
            },
        };
    }

    private sealed class DashboardMetricSnapshot
    {
        public required DashboardUsersSummary Users { get; init; }

        public required DashboardRolesSummary Roles { get; init; }

        public required DashboardPermissionsSummary Permissions { get; init; }

        public required DashboardAccessSummary Access { get; init; }
    }

    private sealed class DashboardPeriodWindow
    {
        private static readonly string[] AllowedPeriods = ["7d", "30d", "90d", "365d", "all", "custom"];

        public required string Period { get; init; }

        public DateTime? StartInclusiveUtc { get; init; }

        public DateTime? EndExclusiveUtc { get; init; }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }

        public bool ComparisonAvailable { get; init; }

        public DashboardSummaryWindow ToResponse()
        {
            return new DashboardSummaryWindow
            {
                Period = Period,
                From = From?.ToString("yyyy-MM-dd"),
                To = To?.ToString("yyyy-MM-dd"),
                ComparisonAvailable = ComparisonAvailable,
            };
        }

        public DashboardPeriodWindow? CreatePreviousWindow()
        {
            if (!ComparisonAvailable || !StartInclusiveUtc.HasValue || !EndExclusiveUtc.HasValue)
            {
                return null;
            }

            var duration = EndExclusiveUtc.Value - StartInclusiveUtc.Value;
            var previousEndExclusiveUtc = StartInclusiveUtc.Value;
            var previousStartInclusiveUtc = previousEndExclusiveUtc - duration;

            return new DashboardPeriodWindow
            {
                Period = Period,
                StartInclusiveUtc = previousStartInclusiveUtc,
                EndExclusiveUtc = previousEndExclusiveUtc,
                From = DateOnly.FromDateTime(previousStartInclusiveUtc),
                To = DateOnly.FromDateTime(previousEndExclusiveUtc.AddDays(-1)),
                ComparisonAvailable = true,
            };
        }

        public static DashboardPeriodWindow Resolve(string? period, string? from, string? to, DateTime nowUtc)
        {
            var normalized = string.IsNullOrWhiteSpace(period)
                ? "30d"
                : period.Trim().ToLowerInvariant();

            var hasFrom = !string.IsNullOrWhiteSpace(from);
            var hasTo = !string.IsNullOrWhiteSpace(to);

            if (normalized == "custom" || hasFrom || hasTo)
            {
                if (!hasFrom || !hasTo)
                {
                    throw new ArgumentException("Debes enviar ambos parámetros from y to para un rango personalizado.");
                }

                if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
                {
                    throw new ArgumentException("Formato de fecha inválido. Usa YYYY-MM-DD.");
                }

                if (fromDate > toDate)
                {
                    throw new ArgumentException("El rango personalizado no puede tener from mayor que to.");
                }

                return CreateCustomWindow(fromDate, toDate);
            }

            var endExclusiveUtc = nowUtc.Date.AddDays(1);

            return normalized switch
            {
                "7d" => CreateRollingWindow("7d", 7, endExclusiveUtc),
                "30d" => CreateRollingWindow("30d", 30, endExclusiveUtc),
                "90d" => CreateRollingWindow("90d", 90, endExclusiveUtc),
                "365d" => CreateRollingWindow("365d", 365, endExclusiveUtc),
                "all" => new DashboardPeriodWindow { Period = "all", ComparisonAvailable = false },
                _ => throw new ArgumentException($"Periodo inválido. Usa uno de: {string.Join(", ", AllowedPeriods)}."),
            };
        }

        private static DashboardPeriodWindow CreateRollingWindow(string period, int days, DateTime endExclusiveUtc)
        {
            var startInclusiveUtc = endExclusiveUtc.AddDays(-days);

            return new DashboardPeriodWindow
            {
                Period = period,
                StartInclusiveUtc = startInclusiveUtc,
                EndExclusiveUtc = endExclusiveUtc,
                From = DateOnly.FromDateTime(startInclusiveUtc),
                To = DateOnly.FromDateTime(endExclusiveUtc.AddDays(-1)),
                ComparisonAvailable = true,
            };
        }

        private static DashboardPeriodWindow CreateCustomWindow(DateOnly from, DateOnly to)
        {
            var startInclusiveUtc = from.ToDateTime(TimeOnly.MinValue);
            var endExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

            return new DashboardPeriodWindow
            {
                Period = "custom",
                StartInclusiveUtc = startInclusiveUtc,
                EndExclusiveUtc = endExclusiveUtc,
                From = from,
                To = to,
                ComparisonAvailable = true,
            };
        }
    }

    public sealed class DashboardSummaryResponse
    {
        public required DashboardSummaryWindow Window { get; init; }

        public DashboardPreviousPeriodSummary? PreviousPeriod { get; init; }

        public required DashboardUsersSummary Users { get; init; }

        public required DashboardRolesSummary Roles { get; init; }

        public required DashboardPermissionsSummary Permissions { get; init; }

        public required DashboardAccessSummary Access { get; init; }

        public DateTime GeneratedAtUtc { get; init; }
    }

    public sealed class DashboardSummaryWindow
    {
        public required string Period { get; init; }

        public string? From { get; init; }

        public string? To { get; init; }

        public bool ComparisonAvailable { get; init; }
    }

    public sealed class DashboardPreviousPeriodSummary
    {
        public required DashboardSummaryWindow Window { get; init; }

        public required DashboardUsersSummary Users { get; init; }

        public required DashboardRolesSummary Roles { get; init; }

        public required DashboardPermissionsSummary Permissions { get; init; }

        public required DashboardAccessSummary Access { get; init; }
    }

    public sealed class DashboardUsersSummary
    {
        public int TotalCount { get; init; }

        public int ActiveCount { get; init; }

        public int PendingCount { get; init; }

        public int InactiveCount { get; init; }

        /// <summary>Usuarios creados dentro de la ventana consultada.</summary>
        public int NewCount { get; init; }
    }

    public sealed class DashboardRolesSummary
    {
        public int TotalCount { get; init; }

        public int ActiveCount { get; init; }

        public int AssignmentsCount { get; init; }
    }

    public sealed class DashboardPermissionsSummary
    {
        public int AssignmentsCount { get; init; }

        public int DistinctCount { get; init; }

        public int ModuleCount { get; init; }
    }

    public sealed class DashboardAccessSummary
    {
        public int ActiveUsersCount { get; init; }

        public int UsersWithRolesCount { get; init; }

        public decimal CoveragePercent { get; init; }
    }
}
