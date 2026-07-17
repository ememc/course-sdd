using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Application.Solicitudes.Queries.GetSolicitudById
{
    public record GetSolicitudByIdQuery : IRequest<SolicitudDetailDto>
    {
        public Guid Id { get; init; }
    }

    public record SolicitudDetailDto(
        Guid Id,
        TipoDetailDto Tipo,
        EmpleadoDetailDto Empleado,
        SupervisorDetailDto? SupervisorAsignado,
        string Estado,
        string CamposDinamicos,
        string? ComentarioSupervisor,
        DateTime FechaCreacion,
        DateTime? FechaEnvio,
        DateTime? FechaResolucion,
        DateTime UltimaModificacion,
        string RowVersion
    );

    public record TipoDetailDto(Guid Id, string Nombre, string CamposDefinicion);
    public record EmpleadoDetailDto(Guid Id, string Nombre, string Area);
    public record SupervisorDetailDto(Guid Id, string Nombre);

    public class GetSolicitudByIdQueryHandler : IRequestHandler<GetSolicitudByIdQuery, SolicitudDetailDto>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetSolicitudByIdQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<SolicitudDetailDto> Handle(GetSolicitudByIdQuery request, CancellationToken cancellationToken)
        {
            var userIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(userIdStr, out var currentUserId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            var currentUser = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == currentUserId && u.Activo, cancellationToken);

            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Usuario no encontrado");
            }

            var solicitud = await _context.Solicitudes
                .Include(s => s.TipoSolicitud)
                .Include(s => s.Empleado)
                    .ThenInclude(e => e.Area)
                .Include(s => s.SupervisorAsignado)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (solicitud == null)
            {
                throw new KeyNotFoundException("Solicitud no encontrada");
            }

            // Role-based visibility check
            if (currentUser.Rol.Nombre == "Empleado" && solicitud.EmpleadoId != currentUserId)
            {
                throw new UnauthorizedAccessException("No tiene permisos para ver esta solicitud");
            }
            else if (currentUser.Rol.Nombre == "Supervisor" &&
                     solicitud.Empleado.AreaId != currentUser.AreaId &&
                     solicitud.SupervisorAsignadoId != currentUserId)
            {
                throw new UnauthorizedAccessException("No tiene permisos para ver esta solicitud");
            }

            return new SolicitudDetailDto(
                solicitud.Id,
                new TipoDetailDto(solicitud.TipoSolicitud.Id, solicitud.TipoSolicitud.Nombre, solicitud.TipoSolicitud.CamposDefinicion),
                new EmpleadoDetailDto(solicitud.EmpleadoId, solicitud.Empleado.Nombre, solicitud.Empleado.Area.Nombre),
                solicitud.SupervisorAsignado != null ? new SupervisorDetailDto(solicitud.SupervisorAsignado.Id, solicitud.SupervisorAsignado.Nombre) : null,
                solicitud.Estado.ToString(),
                solicitud.CamposDinamicos,
                solicitud.ComentarioSupervisor,
                solicitud.FechaCreacion,
                solicitud.FechaEnvio,
                solicitud.FechaResolucion,
                solicitud.UltimaModificacion,
                Convert.ToBase64String(solicitud.RowVersion)
            );
        }
    }
}
