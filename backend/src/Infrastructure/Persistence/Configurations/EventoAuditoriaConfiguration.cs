using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class EventoAuditoriaConfiguration : IEntityTypeConfiguration<EventoAuditoria>
    {
        public void Configure(EntityTypeBuilder<EventoAuditoria> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.EstadoAnterior)
                .HasMaxLength(20);

            builder.Property(x => x.EstadoNuevo)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Accion)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasOne(x => x.Solicitud)
                .WithMany()
                .HasForeignKey(x => x.SolicitudId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade is fine for audit trail if request is physically deleted (drafts cleanup)

            builder.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.SolicitudId, x.FechaHora });
            builder.HasIndex(x => new { x.UsuarioId, x.FechaHora });
        }
    }
}
