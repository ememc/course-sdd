using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.TiposSolicitud.Commands.UpdateTipoSolicitud
{
    public record UpdateTipoSolicitudCommand : IRequest<Unit>
    {
        public Guid Id { get; init; }
        public string? Descripcion { get; init; }
        public string CamposDefinicion { get; init; } = "[]";
    }

    public class UpdateTipoSolicitudCommandHandler : IRequestHandler<UpdateTipoSolicitudCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateTipoSolicitudCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(UpdateTipoSolicitudCommand request, CancellationToken cancellationToken)
        {
            var currentUserIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(currentUserIdStr, out var adminId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            var adminUser = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == adminId && u.Activo, cancellationToken);

            if (adminUser == null || adminUser.Rol.Nombre != "Administrador")
            {
                throw new UnauthorizedAccessException("Solo los administradores pueden modificar tipos de solicitud");
            }

            var tipoSolicitud = await _context.TiposSolicitud
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (tipoSolicitud == null)
            {
                throw new KeyNotFoundException("Tipo de solicitud no encontrado");
            }

            tipoSolicitud.Descripcion = request.Descripcion;
            tipoSolicitud.CamposDefinicion = request.CamposDefinicion;

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
