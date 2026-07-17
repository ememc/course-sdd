using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Solicitudes.Commands.TomarSolicitud
{
    public record TomarSolicitudCommand : IRequest<TomarSolicitudResponse>
    {
        public Guid Id { get; init; }
        public string RowVersion { get; init; } = string.Empty;
    }

    public record TomarSolicitudResponse(
        Guid Id,
        string Estado,
        string SupervisorNombre
    );

    public class TomarSolicitudCommandHandler : IRequestHandler<TomarSolicitudCommand, TomarSolicitudResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public TomarSolicitudCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TomarSolicitudResponse> Handle(TomarSolicitudCommand request, CancellationToken cancellationToken)
        {
            var userIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(userIdStr, out var supervisorId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            var supervisor = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == supervisorId && u.Activo, cancellationToken);

            if (supervisor == null || supervisor.Rol.Nombre != "Supervisor")
            {
                throw new UnauthorizedAccessException("Solo usuarios con el rol de Supervisor pueden tomar solicitudes");
            }

            var solicitud = await _context.Solicitudes
                .Include(s => s.Empleado)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (solicitud == null)
            {
                throw new KeyNotFoundException("Solicitud no encontrada");
            }

            if (solicitud.Estado != EstadoSolicitud.Enviada)
            {
                throw new InvalidOperationException("Solo se pueden tomar solicitudes en estado Enviada");
            }

            // Area constraint: Supervisor must belong to the same Area as the Employee who created it
            if (solicitud.Empleado.AreaId != supervisor.AreaId)
            {
                throw new UnauthorizedAccessException("El supervisor solo puede tomar solicitudes de empleados de su propia área");
            }

            // Concurrency check
            var dbContext = (_context as DbContext);
            if (dbContext != null)
            {
                dbContext.Entry(solicitud).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
            }

            solicitud.Estado = EstadoSolicitud.EnRevision;
            solicitud.SupervisorAsignadoId = supervisorId;

            await _context.SaveChangesAsync(cancellationToken);

            return new TomarSolicitudResponse(
                solicitud.Id,
                solicitud.Estado.ToString(),
                supervisor.Nombre
            );
        }
    }
}
