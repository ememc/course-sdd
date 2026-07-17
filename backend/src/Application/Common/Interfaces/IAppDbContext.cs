using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Usuario> Usuarios { get; }
        DbSet<Rol> Roles { get; }
        DbSet<Area> Areas { get; }
        DbSet<Solicitud> Solicitudes { get; }
        DbSet<TipoSolicitud> TiposSolicitud { get; }
        DbSet<EventoAuditoria> EventosAuditoria { get; }
        DbSet<Notificacion> Notificaciones { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
