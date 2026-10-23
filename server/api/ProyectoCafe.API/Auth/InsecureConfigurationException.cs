namespace ProyectoCafe.API.Auth;

/// <summary>
/// Excepción de configuración insegura que debe abortar el arranque.
/// </summary>
public sealed class InsecureConfigurationException(string message) : Exception(message);
