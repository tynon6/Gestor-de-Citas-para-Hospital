using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;

namespace SanitasFlow.Api.Services;

public static class CatalogSeed
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Especialidades.AnyAsync()) return;

        var especialidades = new[]
        {
            new Especialidad { Id = "general", Nombre = "Cita general", Descripcion = "Consulta de medicina general." },
            new Especialidad { Id = "cardiologia", Nombre = "Cardiología", Descripcion = "Diagnóstico y seguimiento cardiaco." },
            new Especialidad { Id = "dermatologia", Nombre = "Dermatología", Descripcion = "Atención de piel, cabello y uñas." },
            new Especialidad { Id = "pediatria", Nombre = "Pediatría", Descripcion = "Atención médica infantil." },
            new Especialidad { Id = "ginecologia", Nombre = "Ginecología", Descripcion = "Salud reproductiva y seguimiento ginecológico." },
            new Especialidad { Id = "traumatologia", Nombre = "Traumatología", Descripcion = "Lesiones y rehabilitación musculoesquelética." }
        };
        context.Especialidades.AddRange(especialidades);
        context.Procedimientos.AddRange(
            P("gen-consulta", "general", "Consulta general", 30, 450), P("gen-revision", "general", "Revisión rápida", 20, 350), P("gen-seguimiento", "general", "Receta y seguimiento", 15, 300), P("gen-certificado", "general", "Certificado médico", 20, 350),
            P("card-consulta", "cardiologia", "Consulta cardiológica general", 40, 800), P("card-ekg", "cardiologia", "Electrocardiograma", 20, 500), P("card-eco", "cardiologia", "Ecocardiograma", 45, 1500), P("card-esfuerzo", "cardiologia", "Prueba de esfuerzo", 60, 2200), P("card-holter", "cardiologia", "Holter 24 horas (instalación)", 30, 1800),
            P("derm-consulta", "dermatologia", "Consulta dermatológica", 30, 700), P("derm-lunares", "dermatologia", "Revisión de lunares", 30, 650), P("derm-crio", "dermatologia", "Crioterapia", 20, 500), P("derm-biopsia", "dermatologia", "Biopsia de piel", 45, 1400), P("derm-acne", "dermatologia", "Tratamiento de acné", 30, 600),
            P("ped-consulta", "pediatria", "Consulta pediátrica", 30, 600), P("ped-nino-sano", "pediatria", "Control de niño sano", 30, 550), P("ped-vacuna", "pediatria", "Vacunación", 15, 400), P("ped-desarrollo", "pediatria", "Valoración de desarrollo", 40, 700),
            P("gine-consulta", "ginecologia", "Consulta ginecológica", 40, 750), P("gine-papanicolaou", "ginecologia", "Papanicolaou", 20, 500), P("gine-usg", "ginecologia", "Ultrasonido pélvico", 30, 900), P("gine-prenatal", "ginecologia", "Control prenatal", 30, 700), P("gine-colposcopia", "ginecologia", "Colposcopía", 40, 1300),
            P("trauma-consulta", "traumatologia", "Consulta traumatológica", 40, 750), P("trauma-infiltracion", "traumatologia", "Infiltración articular", 30, 1100), P("trauma-postop", "traumatologia", "Revisión postoperatoria", 20, 500), P("trauma-yeso", "traumatologia", "Inmovilización / yeso", 30, 650), P("trauma-rehab", "traumatologia", "Rehabilitación (sesión)", 45, 600)
        );
        await context.SaveChangesAsync();
    }

    private static Procedimiento P(string id, string especialidadId, string nombre, int duracionMin, decimal precio) => new()
    {
        Id = id, EspecialidadId = especialidadId, Nombre = nombre, Descripcion = nombre, DuracionMin = duracionMin, Precio = precio
    };
}
