using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Usuario
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        
        public Guid RolId { get; set; }
        public Rol Rol { get; set; } = null!;

        public Guid? SupervisorId { get; set; }
        public Usuario? Supervisor { get; set; }

        public Guid AreaId { get; set; }
        public Area Area { get; set; } = null!;

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaDesactivacion { get; set; }
    }
}
