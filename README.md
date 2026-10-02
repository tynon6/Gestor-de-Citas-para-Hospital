# SanitasFlow

Sistema de citas médicas con interfaz web, API ASP.NET Core y PostgreSQL.

## Ejecutar localmente

1. Instala .NET 10 y PostgreSQL.
2. Crea una base de datos llamada `SanitasFlow`.
3. Guarda la cadena de conexión fuera del repositorio:

```powershell
dotnet user-secrets --project SanitasFlow.Api set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=SanitasFlow;Username=postgres;Password=TU_CONTRASENA"
```

4. Ejecuta la aplicación en Development para que cree el esquema y cargue el catálogo inicial:

```powershell
dotnet run --project SanitasFlow.Api
```

Abre la dirección local que muestre la terminal (por defecto `http://localhost:5067`).

## Publicar una demo en Render

El repositorio contiene un `Dockerfile` para desplegar el sitio y la API juntos. La base PostgreSQL se aloja aparte en Render.

1. Sube este repositorio a GitHub.
2. En Render, crea una base **PostgreSQL** nueva. Usa una base vacía para el primer despliegue.
3. Crea un **Web Service** conectado a este repositorio y selecciona **Docker** como entorno. Render detectará `Dockerfile` en la raíz.
4. En las variables de entorno del servicio configura:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `Database__EnsureCreated` = `true` (crea las tablas y el catálogo inicial en la base vacía la primera vez)
   - `ConnectionStrings__DefaultConnection` = cadena Npgsql formada con el host interno, puerto, nombre de base, usuario y contraseña que muestra Render. Ejemplo:

```text
Host=HOST_INTERNO;Port=5432;Database=NOMBRE_BASE;Username=USUARIO;Password=CONTRASENA;SSL Mode=Require;Trust Server Certificate=true
```

5. Espera a que termine el despliegue y comparte la URL HTTPS que Render asigna al servicio.

No guardes la cadena de conexión en este repositorio. Configúrala únicamente en las variables privadas del servicio. Para evolucionar el esquema después del primer despliegue, añade migraciones de Entity Framework Core; `EnsureCreated` está pensado aquí para una demo con una base nueva.

## Arquitectura

- `SanitasFlow.Api`: API ASP.NET Core y archivos web estáticos servidos por la misma aplicación.
- `postgresql-schema.sql`: esquema SQL de referencia para configurar PostgreSQL manualmente.
- PostgreSQL conserva pacientes, citas, pagos y el catálogo.
- La autenticación usa una cookie HTTP-only; no existe una tabla de sesiones ni un historial de inicios de sesión.

## Endpoints principales

- `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`
- `PUT /api/pacientes/me`
- `GET /api/citas/disponibilidad`, `GET /api/citas/mias`, `GET /api/citas/{id}`, `POST /api/citas`
