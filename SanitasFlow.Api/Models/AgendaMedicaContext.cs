using Microsoft.EntityFrameworkCore;

namespace SanitasFlow.Api.Models;

public class AgendaMedicaContext(DbContextOptions<AgendaMedicaContext> options) : DbContext(options)
{
    public DbSet<CitaConfirmada> CitasConfirmadas => Set<CitaConfirmada>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CitaConfirmada>(entity =>
        {
            entity.HasIndex(c => c.CitaId).IsUnique();
            entity.Property(c => c.Medico).HasMaxLength(300);
            entity.Property(c => c.Especialidad).HasMaxLength(150);
            entity.Property(c => c.Paciente).HasMaxLength(300);
        });
    }
}
