using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Solicitudes.Commands.AprobarSolicitud
{
    public record AprobarSolicitudCommand : IRequest<AprobarSolicitudResponse>
    {
        public Guid Id { get; init; }
    }

    public record AprobarSolicitudResponse(
        Guid Id,
        string Estado,
        DateTime FechaResolucion
    );

    public class AprobarSolicitudCommandHandler : IRequestHandler<AprobarSolicitudCommand, AprobarSolicitudResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AprobarSolicitudCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AprobarSolicitudResponse> Handle(AprobarSolicitudCommand request, CancellationToken cancellationToken)
        {
            var userIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(userIdStr, out var supervisorId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            var solicitud = await _context.Solicitudes
                .Include(s => s.TipoSolicitud)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (solicitud == null)
            {
                throw new KeyNotFoundException("Solicitud no encontrada");
            }

            if (solicitud.Estado != EstadoSolicitud.EnRevision)
            {
                throw new InvalidOperationException("Solo se pueden aprobar solicitudes en revisión");
            }

            if (solicitud.SupervisorAsignadoId != supervisorId)
            {
                throw new UnauthorizedAccessException("Solo el supervisor asignado puede aprobar esta solicitud");
            }

            solicitud.Estado = EstadoSolicitud.Aprobada;
            solicitud.FechaResolucion = DateTime.UtcNow;

            // Create notification for employee
            var notificacion = new Notificacion
            {
                UsuarioId = solicitud.EmpleadoId,
                Mensaje = $"Tu solicitud de tipo {solicitud.TipoSolicitud.Nombre} ha sido aprobada.",
                Leido = false,
                FechaCreacion = DateTime.UtcNow,
                SolicitudId = solicitud.Id
            };
            _context.Notificaciones.Add(notificacion);

            await _context.SaveChangesAsync(cancellationToken);

            return new AprobarSolicitudResponse(
                solicitud.Id,
                solicitud.Estado.ToString(),
                solicitud.FechaResolucion.Value
            );
        }
    }
}
