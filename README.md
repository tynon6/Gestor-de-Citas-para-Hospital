# SanitasFlow

Sistema web para consultar especialidades médicas y reservar citas en línea.

## Descripción

SanitasFlow permite a los pacientes explorar el catálogo de servicios, crear una cuenta, consultar horarios disponibles y administrar sus citas desde el navegador.

## Equipo

- José Antonio Collazo Hernández
- Iker Gael Cruz Vargas
- María Fernanda Santos Fuentes

## Funcionalidades

- Registro e inicio de sesión de pacientes.
- Acceso de personal médico y enfermería, con usuarios institucionales.
- Consulta de especialidades y procedimientos, con duración y precio.
- Búsqueda de horarios por disponibilidad médica y reserva de citas pendientes.
- Aceptación o rechazo de solicitudes por el médico asignado, con reasignación automática.
- Registro separado de citas confirmadas para la agenda médica.
- Consulta y actualización del perfil del paciente.
- Historial de citas del paciente.
- Registro de donativos en PostgreSQL.

## Tecnologías

- Interfaz: HTML, CSS y JavaScript.
- API: ASP.NET Core 10.
- Persistencia: PostgreSQL con Entity Framework Core y Npgsql.
- Autenticación: cookie HTTP-only y contraseñas almacenadas como hash.

## Estructura

- `SanitasFlow.Api`: API y lógica del sistema.
- `Interfaz`: páginas, estilos, scripts e imágenes del sitio.
- `Base de datos`: esquema SQL de PostgreSQL.
- `Documentación`: documento del proyecto.

## Página web

**Enlace:** [SanitasFlow](https://gestor-de-citas-para-hospital-1.onrender.com)

## Consideraciones

Los pagos y reembolsos son de demostración; no se procesan transacciones bancarias. No se guardan datos de tarjetas. El sistema es académico y no debe utilizarse con datos médicos reales.

## Bases de datos

- `Base de datos/postgresql-schema.sql`: pacientes, personal, especialidades, procedimientos, citas, pagos, rechazos y donativos.
- `Base de datos/postgresql-agenda-schema.sql`: agenda independiente con el resumen de citas confirmadas.
- `Base de datos/postgresql-canceladas-schema.sql`: registro independiente de citas canceladas que estuvieron confirmadas.
- `ConnectionStrings__AgendaConnection`: cadena de conexión de la base PostgreSQL separada para la agenda. Si no se configura, la aplicación usa `DefaultConnection` como respaldo local.
- `ConnectionStrings__CancelledAgendaConnection`: cadena de conexión de la base PostgreSQL de cancelaciones. Si no se configura, en local usa `DefaultConnection` como respaldo.

La primera ejecución con `Database__EnsureCreated=true` crea las tablas faltantes y agrega los campos nuevos sin borrar las citas existentes.

## Acceso de demostración del personal

Las cuentas iniciales se crean al iniciar la API con `Database__EnsureCreated=true`. Configura las contraseñas fuera del repositorio:

- Médicos: usuarios `general01` a `general03`, `cardio01` a `cardio03`, `derma01` a `derma03`, `pedia01` a `pedia03`, `gine01` a `gine03` y `trauma01` a `trauma03`.
- Enfermería: usuario `enfermeria01`.
- Acceso privado: `Interfaz/acceso-medico.html`.

En desarrollo local, la contraseña inicial de médicos es `Local-Medicos-2026!` y la de enfermería `Local-Enfermeria-2026!`. En producción, define `StaffSeed:MedicalPassword` y `StaffSeed:NursePassword` como secretos/variables de entorno antes del primer despliegue; la API no usa las claves de desarrollo allí. Las contraseñas se guardan como hashes en PostgreSQL.
