using System;

namespace Domain.Entities
{
    public class EventoAuditoria
    {
        public Guid Id { get; set; }
        
        public Guid SolicitudId { get; set; }
        public Solicitud Solicitud { get; set; } = null!;

        public Guid UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public string? EstadoAnterior { get; set; }
        public string EstadoNuevo { get; set; } = string.Empty;
        
        public string Accion { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        
        public string? Metadata { get; set; } // JSON
    }
}
