# SanitasFlow

Aplicación web para consultar especialidades médicas y reservar citas en línea.

## Funciones

- Registro de pacientes, inicio y cierre de sesión.
- Consulta de especialidades, procedimientos, precios y duración.
- Consulta de horarios y reserva de citas.
- Consulta del perfil y de las citas del paciente.
- Registro de pagos de demostración; no se conecta a una pasarela bancaria.

## Tecnologías

- HTML, CSS y JavaScript.
- ASP.NET Core 10 y Entity Framework Core.
- PostgreSQL con Npgsql.

## Ejecutar en local

Se requiere .NET 10 y PostgreSQL. Crea una base vacía llamada `SanitasFlow` y guarda la conexión fuera del repositorio:

```powershell
dotnet user-secrets --project SanitasFlow.Api set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=SanitasFlow;Username=postgres;Password=TU_CONTRASENA"
```

Desde la raíz del repositorio, inicia la aplicación:

```powershell
dotnet run --project SanitasFlow.Api
```

En modo `Development`, la aplicación crea las tablas y carga el catálogo inicial. Abre `http://localhost:5067`.

## Despliegue en Render

El `Dockerfile` permite publicar el sitio y la API en un mismo servicio. PostgreSQL se crea por separado en Render.

1. En Render, crea una base PostgreSQL vacía.
2. Crea un **Web Service** conectado a este repositorio y selecciona **Docker**. Deja vacío **Root Directory** para usar el `Dockerfile` de la raíz.
3. Añade estas variables de entorno al servicio:

   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `Database__EnsureCreated` = `true`
   - `ConnectionStrings__DefaultConnection` = cadena de conexión Npgsql con los datos internos de la base de Render.

   Formato:

   ```text
   Host=HOST_INTERNO;Port=5432;Database=NOMBRE_BASE;Username=USUARIO;Password=CONTRASENA;SSL Mode=Require
   ```

4. Despliega el servicio y comparte la URL HTTPS que asigna Render.

La cadena de conexión debe guardarse únicamente en las variables privadas de Render, nunca en GitHub. `EnsureCreated` sirve para iniciar una base vacía; para cambios futuros del esquema conviene añadir migraciones de Entity Framework Core.

## Estructura

- `SanitasFlow.Api/`: API, modelos, servicios y archivos web publicados.
- `postgresql-schema.sql`: esquema SQL de referencia para PostgreSQL.
- Archivos HTML, `app.js`, `styles.css` e imágenes en la raíz: interfaz web que el proyecto copia a `wwwroot` durante la compilación.

La contraseña del paciente se guarda como hash. La sesión usa una cookie HTTP-only; no se registra un historial de inicios de sesión. Este proyecto es demostrativo: no uses datos médicos reales ni información bancaria.
