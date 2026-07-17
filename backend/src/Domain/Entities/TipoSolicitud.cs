using System;

namespace Domain.Entities
{
    public class TipoSolicitud
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string CamposDefinicion { get; set; } = string.Empty; // JSON
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public Guid CreadoPorId { get; set; }
        public Usuario CreadoPor { get; set; } = null!;
    }
}
