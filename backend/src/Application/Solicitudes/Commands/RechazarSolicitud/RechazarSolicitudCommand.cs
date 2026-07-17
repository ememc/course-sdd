using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace Application.Solicitudes.Commands.RechazarSolicitud
{
    public record RechazarSolicitudCommand : IRequest<RechazarSolicitudResponse>
    {
        public Guid Id { get; init; }
        public string Comentario { get; init; } = string.Empty;
    }

    public record RechazarSolicitudResponse(
        Guid Id,
        string Estado,
        string ComentarioSupervisor,
        DateTime FechaResolucion
    );

    public class RechazarSolicitudCommandValidator : AbstractValidator<RechazarSolicitudCommand>
    {
        public RechazarSolicitudCommandValidator()
        {
            RuleFor(x => x.Comentario)
                .NotEmpty().WithMessage("El comentario del supervisor es requerido.")
                .Length(10, 2000).WithMessage("El comentario debe tener entre 10 y 2000 caracteres.");
        }
    }

    public class RechazarSolicitudCommandHandler : IRequestHandler<RechazarSolicitudCommand, RechazarSolicitudResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public RechazarSolicitudCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<RechazarSolicitudResponse> Handle(RechazarSolicitudCommand request, CancellationToken cancellationToken)
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
                throw new InvalidOperationException("Solo se pueden rechazar solicitudes en revisión");
            }

            if (solicitud.SupervisorAsignadoId != supervisorId)
            {
                throw new UnauthorizedAccessException("Solo el supervisor asignado puede rechazar esta solicitud");
            }

            solicitud.Estado = EstadoSolicitud.Rechazada;
            solicitud.ComentarioSupervisor = request.Comentario;
            solicitud.FechaResolucion = DateTime.UtcNow;

            // Create notification for employee
            var notificacion = new Notificacion
            {
                UsuarioId = solicitud.EmpleadoId,
                Mensaje = $"Tu solicitud de tipo {solicitud.TipoSolicitud.Nombre} ha sido rechazada. Motivo: {request.Comentario}",
                Leido = false,
                FechaCreacion = DateTime.UtcNow,
                SolicitudId = solicitud.Id
            };
            _context.Notificaciones.Add(notificacion);

            await _context.SaveChangesAsync(cancellationToken);

            return new RechazarSolicitudResponse(
                solicitud.Id,
                solicitud.Estado.ToString(),
                solicitud.ComentarioSupervisor,
                solicitud.FechaResolucion.Value
            );
        }
    }
}
