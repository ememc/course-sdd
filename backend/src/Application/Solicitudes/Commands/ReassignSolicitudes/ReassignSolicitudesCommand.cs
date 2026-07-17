using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Solicitudes.Commands.ReassignSolicitudes
{
    public record ReassignSolicitudesCommand : IRequest<Unit>
    {
        public List<Guid> SolicitudIds { get; init; } = new();
        public Guid NuevoSupervisorId { get; init; }
    }

    public class ReassignSolicitudesCommandHandler : IRequestHandler<ReassignSolicitudesCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReassignSolicitudesCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(ReassignSolicitudesCommand request, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("Solo los administradores pueden reasignar solicitudes");
            }

            var supervisor = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == request.NuevoSupervisorId && u.Activo, cancellationToken);

            if (supervisor == null || supervisor.Rol.Nombre != "Supervisor")
            {
                throw new InvalidOperationException("El supervisor de destino no es válido o está inactivo");
            }

            var solicitudes = await _context.Solicitudes
                .Include(s => s.Empleado)
                .Where(s => request.SolicitudIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            foreach (var solicitud in solicitudes)
            {
                // Only active requests in Enviada or EnRevision states should be reassigned,
                // but wait: we can reassign drafts if needed, although drafts are private to employee.
                // The spec says "reasignar solicitudes en bloque". Let's update the supervisor for these requests.
                solicitud.SupervisorAsignadoId = request.NuevoSupervisorId;

                // Create notification for the new supervisor
                var notificacion = new Notificacion
                {
                    UsuarioId = request.NuevoSupervisorId,
                    Mensaje = $"Se te ha asignado la solicitud de {solicitud.Empleado.Nombre} para su revisión.",
                    Leido = false,
                    FechaCreacion = DateTime.UtcNow,
                    SolicitudId = solicitud.Id
                };
                _context.Notificaciones.Add(notificacion);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
