using Microsoft.AspNetCore.Authorization;

namespace ProyectoCafe.API.Auth;

public sealed record PermissionRequirement(string Modulo, string Accion) : IAuthorizationRequirement;
