using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;
using SanitasFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);
const string PatientCookie = "PatientCookie";
const string StaffCookie = "StaffCookie";

// Registrar PostgreSQL mediante la cadena de conexión configurada fuera del repositorio.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión DefaultConnection.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
var agendaConnectionString = builder.Configuration.GetConnectionString("AgendaConnection");
if (string.IsNullOrWhiteSpace(agendaConnectionString)) agendaConnectionString = connectionString;
builder.Services.AddDbContext<AgendaMedicaContext>(options => options.UseNpgsql(agendaConnectionString));
var cancellationsConnectionString = builder.Configuration.GetConnectionString("CancelledAgendaConnection");
if (string.IsNullOrWhiteSpace(cancellationsConnectionString)) cancellationsConnectionString = connectionString;
builder.Services.AddDbContext<AgendaCancelacionesContext>(options => options.UseNpgsql(cancellationsConnectionString));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = PatientCookie;
        options.DefaultChallengeScheme = PatientCookie;
        options.DefaultSignInScheme = PatientCookie;
    })
    .AddCookie(PatientCookie, options =>
    {
        options.Cookie.Name = "SanitasFlow.PatientAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    })
    .AddCookie(StaffCookie, options =>
    {
        options.Cookie.Name = "SanitasFlow.StaffAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PatientOnly", policy => policy
        .AddAuthenticationSchemes(PatientCookie)
        .RequireRole("Paciente"));
    options.AddPolicy("StaffOnly", policy => policy
        .AddAuthenticationSchemes(StaffCookie)
        .RequireRole("Medico", "Enfermero"));
    options.AddPolicy("DoctorOnly", policy => policy
        .AddAuthenticationSchemes(StaffCookie)
        .RequireRole("Medico"));
    options.AddPolicy("NurseOnly", policy => policy
        .AddAuthenticationSchemes(StaffCookie)
        .RequireRole("Enfermero"));
});
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins("http://localhost:5500", "http://127.0.0.1:5500", "https://localhost:7162")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddScoped<CitaService>();
builder.Services.AddScoped<CancellationPolicyService>();
builder.Services.AddScoped<AgendaMedicaService>();
builder.Services.AddSingleton<PasswordService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// (Opcional) Comentado temporalmente por incompatibilidad con el SDK de .NET:
// builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:EnsureCreated"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE "Pacientes" ADD COLUMN IF NOT EXISTS "NombreUsuario" character varying(40);
        UPDATE "Pacientes" SET "NombreUsuario" = 'paciente' || "Id" WHERE "NombreUsuario" IS NULL OR "NombreUsuario" = '';
        ALTER TABLE "Pacientes" ALTER COLUMN "NombreUsuario" SET NOT NULL;
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Pacientes_NombreUsuario" ON "Pacientes" ("NombreUsuario");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Pacientes_Correo" ON "Pacientes" ("Correo");
        ALTER TABLE "Medicos" ADD COLUMN IF NOT EXISTS "ApellidoPaterno" text NOT NULL DEFAULT '';
        ALTER TABLE "Medicos" ADD COLUMN IF NOT EXISTS "ApellidoMaterno" text NOT NULL DEFAULT '';
        ALTER TABLE "Medicos" ADD COLUMN IF NOT EXISTS "Edad" integer NOT NULL DEFAULT 35;
        ALTER TABLE "Medicos" ADD COLUMN IF NOT EXISTS "Sexo" text NOT NULL DEFAULT '';
        ALTER TABLE "Medicos" ADD COLUMN IF NOT EXISTS "EspecialidadId" character varying(450);
        ALTER TABLE "Medicos" ADD COLUMN IF NOT EXISTS "Especialidad" text;
        ALTER TABLE "Medicos" ALTER COLUMN "Especialidad" DROP NOT NULL;
        ALTER TABLE "Medicos" ADD COLUMN IF NOT EXISTS "NombreUsuario" character varying(40);
        ALTER TABLE "Medicos" ADD COLUMN IF NOT EXISTS "PasswordHash" character varying(512) NOT NULL DEFAULT '';
        UPDATE "Medicos" SET "ApellidoPaterno" = split_part("Nombre", ' ', 2) WHERE "ApellidoPaterno" = '' AND cardinality(string_to_array("Nombre", ' ')) > 1;
        UPDATE "Medicos" SET "Nombre" = split_part("Nombre", ' ', 1) WHERE cardinality(string_to_array("Nombre", ' ')) > 1;
        UPDATE "Medicos" SET "EspecialidadId" = CASE lower("Especialidad")
            WHEN 'cardiología' THEN 'cardiologia' WHEN 'cardiologia' THEN 'cardiologia'
            WHEN 'dermatología' THEN 'dermatologia' WHEN 'dermatologia' THEN 'dermatologia'
            WHEN 'pediatría' THEN 'pediatria' WHEN 'pediatria' THEN 'pediatria'
            WHEN 'ginecología' THEN 'ginecologia' WHEN 'ginecologia' THEN 'ginecologia'
            WHEN 'traumatología' THEN 'traumatologia' WHEN 'traumatologia' THEN 'traumatologia'
            ELSE 'general' END WHERE "EspecialidadId" IS NULL OR "EspecialidadId" = '';
        UPDATE "Medicos" SET "NombreUsuario" = 'medico' || "Id" WHERE "NombreUsuario" IS NULL OR "NombreUsuario" = '';
        ALTER TABLE "Medicos" ALTER COLUMN "EspecialidadId" SET NOT NULL;
        ALTER TABLE "Medicos" ALTER COLUMN "NombreUsuario" SET NOT NULL;
        DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_Medicos_Edad') THEN
                ALTER TABLE "Medicos" ADD CONSTRAINT "CK_Medicos_Edad" CHECK ("Edad" BETWEEN 26 AND 60);
            END IF;
        END $$;
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Medicos_NombreUsuario" ON "Medicos" ("NombreUsuario");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Medicos_Correo" ON "Medicos" ("Correo");
        DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Medicos_Especialidades_EspecialidadId') THEN
                ALTER TABLE "Medicos" ADD CONSTRAINT "FK_Medicos_Especialidades_EspecialidadId" FOREIGN KEY ("EspecialidadId") REFERENCES "Especialidades"("Id") ON DELETE RESTRICT;
            END IF;
        END $$;
        CREATE TABLE IF NOT EXISTS "Enfermeros" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "Nombre" text NOT NULL,
            "ApellidoPaterno" text NOT NULL,
            "ApellidoMaterno" text NOT NULL,
            "Edad" integer NOT NULL,
            "Correo" character varying(320) NOT NULL UNIQUE,
            "NombreUsuario" character varying(40) NOT NULL UNIQUE,
            "PasswordHash" character varying(512) NOT NULL
        );
        ALTER TABLE "Citas" ADD COLUMN IF NOT EXISTS "MedicoId" integer;
        DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Citas_Medicos_MedicoId') THEN
                ALTER TABLE "Citas" ADD CONSTRAINT "FK_Citas_Medicos_MedicoId" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id") ON DELETE SET NULL;
            END IF;
        END $$;
        ALTER TABLE "Citas" ADD COLUMN IF NOT EXISTS "MotivoCancelacion" character varying(1000);
        ALTER TABLE "Citas" ADD COLUMN IF NOT EXISTS "DeseaReagendar" boolean NOT NULL DEFAULT FALSE;
        ALTER TABLE "Citas" ADD COLUMN IF NOT EXISTS "CanceladaEnUtc" timestamp with time zone;
        ALTER TABLE "Citas" ADD COLUMN IF NOT EXISTS "PorcentajeReembolso" integer;
        ALTER TABLE "Citas" ADD COLUMN IF NOT EXISTS "MontoReembolso" numeric(10,2);
        CREATE TABLE IF NOT EXISTS "Donativos" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "Monto" numeric(10,2) NOT NULL CHECK ("Monto" >= 10 AND "Monto" <= 100000),
            "Moneda" character varying(3) NOT NULL DEFAULT 'MXN',
            "Estado" character varying(32) NOT NULL DEFAULT 'Registrado',
            "CreadoEnUtc" timestamp with time zone NOT NULL
        );
        CREATE TABLE IF NOT EXISTS "RechazosCita" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "CitaId" integer NOT NULL REFERENCES "Citas"("Id") ON DELETE CASCADE,
            "MedicoId" integer NOT NULL REFERENCES "Medicos"("Id") ON DELETE CASCADE,
            "RechazadaEnUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "UQ_RechazosCita_CitaId_MedicoId" UNIQUE ("CitaId", "MedicoId")
        );
        """);
    var medicalAgendaDb = scope.ServiceProvider.GetRequiredService<AgendaMedicaContext>();
    await medicalAgendaDb.Database.EnsureCreatedAsync();
    await medicalAgendaDb.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "CitasConfirmadas" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "CitaId" integer NOT NULL UNIQUE,
            "Medico" character varying(300) NOT NULL,
            "Especialidad" character varying(150) NOT NULL,
            "Paciente" character varying(300) NOT NULL,
            "Fecha" date NOT NULL,
            "InicioMin" integer NOT NULL,
            "FinMin" integer NOT NULL,
            "ConfirmadaEnUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "CK_CitasConfirmadas_Horario" CHECK ("FinMin" > "InicioMin")
        );
        """);
    var cancelledAgendaDb = scope.ServiceProvider.GetRequiredService<AgendaCancelacionesContext>();
    await cancelledAgendaDb.Database.EnsureCreatedAsync();
    await cancelledAgendaDb.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "CitasCanceladas" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "CitaId" integer NOT NULL UNIQUE,
            "Estado" character varying(32) NOT NULL DEFAULT 'Cancelada',
            "MedicoId" integer NOT NULL,
            "Medico" character varying(300) NOT NULL,
            "Especialidad" character varying(150) NOT NULL,
            "Paciente" character varying(300) NOT NULL,
            "Procedimiento" character varying(200) NOT NULL,
            "Fecha" date NOT NULL,
            "InicioMin" integer NOT NULL,
            "FinMin" integer NOT NULL,
            "MotivoCancelacion" character varying(1000),
            "CanceladaEnUtc" timestamp with time zone NOT NULL,
            "PorcentajeReembolso" integer NOT NULL,
            "MontoReembolso" numeric(10,2) NOT NULL,
            CONSTRAINT "CK_CitasCanceladas_Horario" CHECK ("FinMin" > "InicioMin")
        );
        """);
    await cancelledAgendaDb.Database.ExecuteSqlRawAsync("""
        ALTER TABLE "CitasCanceladas"
        ADD COLUMN IF NOT EXISTS "Estado" character varying(32) NOT NULL DEFAULT 'Cancelada';
        """);

    var previouslyCancelledAppointments = await db.Citas.AsNoTracking()
        .Include(c => c.Paciente).Include(c => c.Especialidad).Include(c => c.Procedimiento)
        .Where(c => c.Estado == "Cancelada" && c.MedicoId.HasValue)
        .ToListAsync();
    foreach (var cancelledAppointment in previouslyCancelledAppointments)
    {
        var confirmedRecord = await medicalAgendaDb.CitasConfirmadas
            .SingleOrDefaultAsync(record => record.CitaId == cancelledAppointment.Id);
        if (confirmedRecord is null) continue;

        var alreadyArchived = await cancelledAgendaDb.CitasCanceladas
            .AnyAsync(record => record.CitaId == cancelledAppointment.Id);
        if (!alreadyArchived)
        {
            cancelledAgendaDb.CitasCanceladas.Add(new CitaCancelada
            {
                CitaId = cancelledAppointment.Id,
                Estado = "Cancelada",
                MedicoId = cancelledAppointment.MedicoId!.Value,
                Medico = confirmedRecord.Medico,
                Especialidad = confirmedRecord.Especialidad,
                Paciente = $"{cancelledAppointment.Paciente.Nombres} {cancelledAppointment.Paciente.ApellidoPaterno} {cancelledAppointment.Paciente.ApellidoMaterno}".Trim(),
                Procedimiento = cancelledAppointment.Procedimiento.Nombre,
                Fecha = confirmedRecord.Fecha,
                InicioMin = confirmedRecord.InicioMin,
                FinMin = confirmedRecord.FinMin,
                MotivoCancelacion = cancelledAppointment.MotivoCancelacion,
                CanceladaEnUtc = cancelledAppointment.CanceladaEnUtc ?? DateTime.UtcNow,
                PorcentajeReembolso = cancelledAppointment.PorcentajeReembolso ?? 0,
                MontoReembolso = cancelledAppointment.MontoReembolso ?? 0m
            });
        }
        medicalAgendaDb.CitasConfirmadas.Remove(confirmedRecord);
    }
    await cancelledAgendaDb.SaveChangesAsync();
    await medicalAgendaDb.SaveChangesAsync();

    await CatalogSeed.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<PasswordService>(),
        builder.Configuration["StaffSeed:MedicalPassword"] ?? (app.Environment.IsDevelopment() ? "Local-Medicos-2026!" : null),
        builder.Configuration["StaffSeed:NursePassword"] ?? (app.Environment.IsDevelopment() ? "Local-Enfermeria-2026!" : null));
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/agenda-medica.html", StringComparison.OrdinalIgnoreCase))
    {
        var staffSession = await context.AuthenticateAsync(StaffCookie);
        var hasStaffRole = staffSession.Succeeded &&
            (staffSession.Principal?.IsInRole("Medico") == true || staffSession.Principal?.IsInRole("Enfermero") == true);
        if (!hasStaffRole)
        {
            context.Response.Redirect("/acceso-medico.html");
            return;
        }
    }

    await next();
});
app.UseStaticFiles();
app.UseCors("frontend");
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
