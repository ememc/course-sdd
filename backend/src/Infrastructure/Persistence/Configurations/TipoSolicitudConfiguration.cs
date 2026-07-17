using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class TipoSolicitudConfiguration : IEntityTypeConfiguration<TipoSolicitud>
    {
        public void Configure(EntityTypeBuilder<TipoSolicitud> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(x => x.Nombre)
                .IsUnique();

            builder.Property(x => x.Descripcion)
                .HasMaxLength(1000);

            builder.Property(x => x.CamposDefinicion)
                .IsRequired();

            builder.HasOne(x => x.CreadoPor)
                .WithMany()
                .HasForeignKey(x => x.CreadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Activo);
        }
    }
}
