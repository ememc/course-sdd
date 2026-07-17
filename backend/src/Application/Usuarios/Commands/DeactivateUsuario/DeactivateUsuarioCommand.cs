using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usuarios.Commands.DeactivateUsuario
{
    public record DeactivateUsuarioCommand : IRequest<DeactivateUsuarioResponse>
    {
        public Guid UsuarioId { get; init; }
        public bool Activo { get; init; }
    }

    public record DeactivateUsuarioResponse(
        bool Success,
        string Message,
        List<PendingSolicitudDto>? PendingSolicitudes = null
    );

    public record PendingSolicitudDto(
        Guid Id,
        string EmpleadoNombre,
        string TipoNombre,
        string Estado,
        DateTime FechaCreacion
    );

    public class DeactivateUsuarioCommandHandler : IRequestHandler<DeactivateUsuarioCommand, DeactivateUsuarioResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeactivateUsuarioCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<DeactivateUsuarioResponse> Handle(DeactivateUsuarioCommand request, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("Solo los administradores pueden activar/desactivar usuarios");
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == request.UsuarioId, cancellationToken);

            if (usuario == null)
            {
                throw new KeyNotFoundException("Usuario no encontrado");
            }

            // If we are deactivating a supervisor, check for active requests (Enviada or EnRevision)
            if (!request.Activo && usuario.Rol.Nombre == "Supervisor")
            {
                var activeRequests = await _context.Solicitudes
                    .Include(s => s.Empleado)
                    .Include(s => s.TipoSolicitud)
                    .Where(s => s.SupervisorAsignadoId == request.UsuarioId &&
                               (s.Estado == EstadoSolicitud.Enviada || s.Estado == EstadoSolicitud.EnRevision))
                    .ToListAsync(cancellationToken);

                if (activeRequests.Any())
                {
                    var pendingList = activeRequests.Select(s => new PendingSolicitudDto(
                        s.Id,
                        s.Empleado.Nombre,
                        s.TipoSolicitud.Nombre,
                        s.Estado.ToString(),
                        s.FechaCreacion
                    )).ToList();

                    return new DeactivateUsuarioResponse(
                        false,
                        "No se puede desactivar el supervisor porque tiene solicitudes activas pendientes. Por favor reasígnelas en bloque.",
                        pendingList
                    );
                }
            }

            usuario.Activo = request.Activo;
            if (!request.Activo)
            {
                usuario.FechaDesactivacion = DateTime.UtcNow;
            }
            else
            {
                usuario.FechaDesactivacion = null;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new DeactivateUsuarioResponse(true, $"Usuario {(request.Activo ? "activado" : "desactivado")} exitosamente");
        }
    }
}
