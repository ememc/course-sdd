using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class SolicitudConfiguration : IEntityTypeConfiguration<Solicitud>
    {
        public void Configure(EntityTypeBuilder<Solicitud> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.Estado)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.CamposDinamicos)
                .IsRequired();

            builder.Property(x => x.ComentarioSupervisor)
                .HasMaxLength(2000);

            builder.Property(x => x.RowVersion)
                .IsRowVersion();

            builder.HasOne(x => x.TipoSolicitud)
                .WithMany()
                .HasForeignKey(x => x.TipoSolicitudId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empleado)
                .WithMany()
                .HasForeignKey(x => x.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SupervisorAsignado)
                .WithMany()
                .HasForeignKey(x => x.SupervisorAsignadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.EmpleadoId, x.FechaCreacion });
            builder.HasIndex(x => new { x.SupervisorAsignadoId, x.Estado });
            builder.HasIndex(x => new { x.Estado, x.TipoSolicitudId });
            builder.HasIndex(x => new { x.FechaCreacion, x.Estado });
        }
    }
}
