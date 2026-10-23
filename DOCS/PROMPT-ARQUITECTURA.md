# Prompt de arquitectura

Trabaja en ProyectoCafe, respetando la arquitectura, tecnologías y convenciones existentes, sin reestructurar el proyecto.

Mantén la separación Domain → Application → Infrastructure → API; reutiliza los DTOs, servicios, permisos y componentes existentes. En el frontend, respeta Next.js, apiFetch, getRuntimeConfig(), useAuth().has(), el sistema visual y las traducciones es/en/pt/fr. Usa SQL Server y las migraciones EF existentes. No reincorpores módulos retirados ni alteres la seguridad sin justificación.

Realiza cambios mínimos, actualiza las pruebas y documentación necesarias, y ejecuta dotnet build, dotnet test, npm run type-check y npm run lint. Si hay ambigüedades importantes, consulta antes de improvisar.