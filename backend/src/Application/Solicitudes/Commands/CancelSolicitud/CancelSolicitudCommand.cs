using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Solicitudes.Commands.CancelSolicitud
{
    public record CancelSolicitudCommand : IRequest<CancelSolicitudResponse>
    {
        public Guid Id { get; init; }
    }

    public record CancelSolicitudResponse(
        Guid Id,
        string Estado,
        DateTime FechaResolucion
    );

    public class CancelSolicitudCommandHandler : IRequestHandler<CancelSolicitudCommand, CancelSolicitudResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CancelSolicitudCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CancelSolicitudResponse> Handle(CancelSolicitudCommand request, CancellationToken cancellationToken)
        {
            var userIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(userIdStr, out var empleadoId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            var solicitud = await _context.Solicitudes
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (solicitud == null)
            {
                throw new KeyNotFoundException("Solicitud no encontrada");
            }

            if (solicitud.EmpleadoId != empleadoId)
            {
                throw new UnauthorizedAccessException("No tiene permisos para cancelar esta solicitud");
            }

            if (solicitud.Estado != EstadoSolicitud.Enviada)
            {
                throw new InvalidOperationException("Solo se pueden cancelar solicitudes en estado Enviada");
            }

            solicitud.Estado = EstadoSolicitud.Cancelada;
            solicitud.FechaResolucion = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new CancelSolicitudResponse(
                solicitud.Id,
                solicitud.Estado.ToString(),
                solicitud.FechaResolucion.Value
            );
        }
    }
}
