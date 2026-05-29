# Windows env vars (ProyectoCafe)

Recomendado: configurar secrets como variables de entorno de usuario/máquina en lugar de commitear valores.

Ejemplos (PowerShell):

- `setx Database__ConnectionString "Server=...;Database=...;..."`
- `setx NEXT_PUBLIC_PROYECTOCAFE_API_URL "http://localhost:5000"`
