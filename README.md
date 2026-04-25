# Plataforma de Créditos - Examen Parcial

Sistema web para la gestión y evaluación de solicitudes de crédito, desarrollado bajo arquitectura MVC.

## Stack Tecnológico

- **Framework:** ASP.NET Core MVC (.NET 8)
- **Base de Datos:** SQLite (Entorno de Desarrollo) / PostgreSQL (Producción)
- **Caché y Sesión:** Redis
- **Infraestructura:** Render.com (Docker)

## Configuración Local

1. **Clonar el repositorio.**
2. **Ejecutar migraciones:** `dotnet ef database update`.
3. **Ejecutar:** `dotnet run`.

## Variables de Entorno en Producción (Render)

Para el despliegue en Render, asegúrate de configurar las siguientes variables de entorno:

- `ASPNETCORE_ENVIRONMENT`: `Production`
- `ConnectionStrings__PostgresConnection`: `Host=...;Database=...;Username=...;Password=...;Port=5432;SSL Mode=Require;Trust Server Certificate=true;`
- `Redis__ConnectionString`: `[Tu cadena de conexión de Redis]`

## Usuarios de Prueba

- **Analista:** `analista@banco.com` / Contraseña: `Password123!`

## URL de Despliegue

https://plataformacreditos-3pof.onrender.com
