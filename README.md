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
- Consulta de especialidades y procedimientos, con duración y precio.
- Búsqueda de horarios disponibles y reserva de citas.
- Consulta y actualización del perfil del paciente.
- Historial de citas del paciente.

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

**Enlace:** [SanitasFlow](https://gestor-de-citas-para-hospital.onrender.com/)

## Consideraciones

Los pagos se registran como demostración; no se procesan transacciones bancarias. El sistema es académico y no debe utilizarse con datos médicos reales.
