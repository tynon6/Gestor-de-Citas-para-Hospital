using Microsoft.EntityFrameworkCore;

namespace SanitasFlow.Api.Models;

public class AgendaCancelacionesContext(DbContextOptions<AgendaCancelacionesContext> options) : DbContext(options)
{
    public DbSet<CitaCancelada> CitasCanceladas => Set<CitaCancelada>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CitaCancelada>(entity =>
        {
            entity.HasIndex(c => c.CitaId).IsUnique();
            entity.Property(c => c.Estado).HasMaxLength(32);
            entity.Property(c => c.Medico).HasMaxLength(300);
            entity.Property(c => c.Especialidad).HasMaxLength(150);
            entity.Property(c => c.Paciente).HasMaxLength(300);
            entity.Property(c => c.Procedimiento).HasMaxLength(200);
            entity.Property(c => c.MotivoCancelacion).HasMaxLength(1000);
            entity.Property(c => c.MontoReembolso).HasPrecision(10, 2);
        });
    }
}
