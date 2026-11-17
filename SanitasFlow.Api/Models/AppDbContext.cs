using Microsoft.EntityFrameworkCore;
namespace SanitasFlow.Api.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Medico> Medicos => Set<Medico>();
        public DbSet<Enfermero> Enfermeros => Set<Enfermero>();
        public DbSet<Paciente> Pacientes => Set<Paciente>();
        public DbSet<Especialidad> Especialidades => Set<Especialidad>();
        public DbSet<Procedimiento> Procedimientos => Set<Procedimiento>();
        public DbSet<Cita> Citas => Set<Cita>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<Donativo> Donativos => Set<Donativo>();
        public DbSet<RechazoCita> RechazosCita => Set<RechazoCita>();
        public DbSet<CuentaMaestra> CuentasMaestras => Set<CuentaMaestra>();
        public DbSet<SolicitudBajaMedico> SolicitudesBajaMedico => Set<SolicitudBajaMedico>();
        public DbSet<MovimientoPersonal> MovimientosPersonal => Set<MovimientoPersonal>();
        public DbSet<MedicoBaja> MedicosBaja => Set<MedicoBaja>();
        public DbSet<MedicoBajaCita> MedicosBajaCitas => Set<MedicoBajaCita>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.HasIndex(p => p.Correo).IsUnique();
                entity.HasIndex(p => p.NombreUsuario).IsUnique();
                entity.Property(p => p.Correo).HasMaxLength(320);
                entity.Property(p => p.PasswordHash).HasMaxLength(512);
            });

            modelBuilder.Entity<Medico>(entity =>
            {
                entity.HasIndex(m => m.Correo).IsUnique();
                entity.HasIndex(m => m.NombreUsuario).IsUnique();
                entity.Property(m => m.NombreUsuario).HasMaxLength(40);
                entity.Property(m => m.Correo).HasMaxLength(320);
                entity.Property(m => m.Activo).HasDefaultValue(true);
            });

            modelBuilder.Entity<Enfermero>(entity =>
            {
                entity.HasIndex(e => e.Correo).IsUnique();
                entity.HasIndex(e => e.NombreUsuario).IsUnique();
                entity.Property(e => e.Correo).HasMaxLength(320);
                entity.Property(e => e.NombreUsuario).HasMaxLength(40);
                entity.Property(e => e.PasswordHash).HasMaxLength(512);
                entity.Property(e => e.Activo).HasDefaultValue(true);
            });

            modelBuilder.Entity<Especialidad>().HasKey(e => e.Id);
            modelBuilder.Entity<Procedimiento>().HasKey(p => p.Id);
            modelBuilder.Entity<Procedimiento>()
                .HasOne(p => p.Especialidad)
                .WithMany(e => e.Procedimientos)
                .HasForeignKey(p => p.EspecialidadId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cita>(entity =>
            {
                entity.HasIndex(c => new { c.EspecialidadId, c.Fecha });
                entity.Property(c => c.Precio).HasPrecision(10, 2);
                entity.Property(c => c.Estado).HasMaxLength(32);
                entity.Property(c => c.MotivoCancelacion).HasMaxLength(1000);
                entity.Property(c => c.MontoReembolso).HasPrecision(10, 2);
                entity.Property(c => c.AvisoReasignacion).HasMaxLength(500);
                entity.HasOne(c => c.Medico).WithMany().HasForeignKey(c => c.MedicoId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(c => c.Paciente).WithMany(p => p.Citas).HasForeignKey(c => c.PacienteId);
                entity.HasOne(c => c.Especialidad).WithMany().HasForeignKey(c => c.EspecialidadId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.Procedimiento).WithMany().HasForeignKey(c => c.ProcedimientoId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Pago>(entity =>
            {
                entity.Property(p => p.Monto).HasPrecision(10, 2);
                entity.HasOne(p => p.Cita).WithMany(c => c.Pagos).HasForeignKey(p => p.CitaId);
            });

            modelBuilder.Entity<Donativo>(entity =>
            {
                entity.Property(d => d.Monto).HasPrecision(10, 2);
                entity.Property(d => d.Moneda).HasMaxLength(3);
                entity.Property(d => d.Estado).HasMaxLength(32);
            });

            modelBuilder.Entity<RechazoCita>(entity =>
            {
                entity.HasIndex(r => new { r.CitaId, r.MedicoId }).IsUnique();
                entity.HasOne<Cita>().WithMany().HasForeignKey(r => r.CitaId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Medico>().WithMany().HasForeignKey(r => r.MedicoId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CuentaMaestra>(entity =>
            {
                entity.ToTable("CuentasMaestras", table => table.HasCheckConstraint("CK_CuentasMaestras_Unica", "\"Id\" = 1"));
                entity.HasKey(account => account.Id);
                entity.Property(account => account.NombreUsuario).HasMaxLength(40).IsRequired();
                entity.Property(account => account.PasswordHash).HasMaxLength(512).IsRequired();
                entity.HasIndex(account => account.NombreUsuario).IsUnique();
            });
            modelBuilder.Entity<SolicitudBajaMedico>(entity =>
            {
                entity.Property(item => item.MedicoNombre).HasMaxLength(300);
                entity.Property(item => item.EnfermeroNombre).HasMaxLength(300);
                entity.Property(item => item.Motivo).HasMaxLength(1000);
                entity.Property(item => item.Estado).HasMaxLength(24);
                entity.HasIndex(item => new { item.MedicoId, item.Estado });
            });
            modelBuilder.Entity<MovimientoPersonal>(entity =>
            {
                entity.Property(item => item.ActorRol).HasMaxLength(30);
                entity.Property(item => item.ActorUsuario).HasMaxLength(80);
                entity.Property(item => item.ActorNombre).HasMaxLength(300);
                entity.Property(item => item.Accion).HasMaxLength(80);
                entity.Property(item => item.DestinoTipo).HasMaxLength(40);
                entity.Property(item => item.DestinoNombre).HasMaxLength(300);
                entity.Property(item => item.Detalle).HasMaxLength(1000);
                entity.HasIndex(item => item.FechaUtc);
            });
            modelBuilder.Entity<MedicoBaja>(entity =>
            {
                entity.Property(item => item.Nombre).HasMaxLength(300);
                entity.Property(item => item.NombreUsuario).HasMaxLength(40);
                entity.Property(item => item.Correo).HasMaxLength(320);
                entity.Property(item => item.EspecialidadId).HasMaxLength(100);
                entity.Property(item => item.Motivo).HasMaxLength(1000);
                entity.HasIndex(item => item.MedicoOriginalId);
            });
            modelBuilder.Entity<MedicoBajaCita>(entity =>
            {
                entity.Property(item => item.Paciente).HasMaxLength(300);
                entity.Property(item => item.Especialidad).HasMaxLength(150);
                entity.Property(item => item.Procedimiento).HasMaxLength(200);
                entity.Property(item => item.MedicoAnterior).HasMaxLength(300);
                entity.Property(item => item.MedicoDestino).HasMaxLength(300);
                entity.Property(item => item.Resultado).HasMaxLength(30);
                entity.Property(item => item.MontoReembolso).HasPrecision(10, 2);
                entity.HasIndex(item => item.CitaId);
            });
        }
    }
}
