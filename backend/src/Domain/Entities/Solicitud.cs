using Domain.Enums;
using System;

namespace Domain.Entities
{
    public class Solicitud
    {
        public Guid Id { get; set; }
        
        public Guid TipoSolicitudId { get; set; }
        public TipoSolicitud TipoSolicitud { get; set; } = null!;

        public Guid EmpleadoId { get; set; }
        public Usuario Empleado { get; set; } = null!;

        public Guid? SupervisorAsignadoId { get; set; }
        public Usuario? SupervisorAsignado { get; set; }

        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Borrador;
        
        public string CamposDinamicos { get; set; } = "{}"; // JSON
        
        public string? ComentarioSupervisor { get; set; }
        
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public DateTime UltimaModificacion { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
