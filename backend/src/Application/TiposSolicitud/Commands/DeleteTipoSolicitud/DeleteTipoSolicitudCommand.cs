using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.TiposSolicitud.Commands.DeleteTipoSolicitud
{
    public record DeleteTipoSolicitudCommand : IRequest<Unit>
    {
        public Guid Id { get; init; }
    }

    public class DeleteTipoSolicitudCommandHandler : IRequestHandler<DeleteTipoSolicitudCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteTipoSolicitudCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(DeleteTipoSolicitudCommand request, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("Solo los administradores pueden eliminar tipos de solicitud");
            }

            var tipoSolicitud = await _context.TiposSolicitud
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (tipoSolicitud == null)
            {
                throw new KeyNotFoundException("Tipo de solicitud no encontrado");
            }

            // FR-006: Impedir eliminar tipos de solicitud que tengan solicitudes asociadas
            var tieneSolicitudes = await _context.Solicitudes
                .AnyAsync(s => s.TipoSolicitudId == request.Id, cancellationToken);

            if (tieneSolicitudes)
            {
                throw new InvalidOperationException("No se puede eliminar este tipo de solicitud porque tiene solicitudes asociadas. En su lugar, desactívelo.");
            }

            _context.TiposSolicitud.Remove(tipoSolicitud);
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
