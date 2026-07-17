using System;

namespace Domain.Entities
{
    public class Notificacion
    {
        public Guid Id { get; set; }
        
        public Guid UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public string Mensaje { get; set; } = string.Empty;
        public bool Leido { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public Guid? SolicitudId { get; set; }
        public Solicitud? Solicitud { get; set; }
    }
}
