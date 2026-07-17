using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Solicitudes.Queries.GetSolicitudes
{
    public record GetSolicitudesQuery : IRequest<GetSolicitudesResponse>
    {
        public string? Estado { get; init; }
        public Guid? TipoSolicitudId { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public record GetSolicitudesResponse(
        List<SolicitudDto> Data,
        int Total,
        int Page,
        int PageSize
    );

    public record SolicitudDto(
        Guid Id,
        TipoDto Tipo,
        EmpleadoDto Empleado,
        SupervisorDto? SupervisorAsignado,
        string Estado,
        DateTime FechaCreacion,
        DateTime? FechaEnvio,
        DateTime? FechaResolucion,
        string RowVersion
    );

    public record TipoDto(Guid Id, string Nombre);
    public record EmpleadoDto(Guid Id, string Nombre);
    public record SupervisorDto(Guid Id, string Nombre);

    public class GetSolicitudesQueryHandler : IRequestHandler<GetSolicitudesQuery, GetSolicitudesResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetSolicitudesQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetSolicitudesResponse> Handle(GetSolicitudesQuery request, CancellationToken cancellationToken)
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

            var query = _context.Solicitudes
                .Include(s => s.TipoSolicitud)
                .Include(s => s.Empleado)
                .Include(s => s.SupervisorAsignado)
                .AsNoTracking();

            // Role-based visibility scoping
            if (currentUser.Rol.Nombre == "Empleado")
            {
                query = query.Where(s => s.EmpleadoId == currentUserId);
            }
            else if (currentUser.Rol.Nombre == "Supervisor")
            {
                // Supervisor: can see requests from employees in their organizational area,
                // or requests explicitly assigned to them.
                query = query.Where(s => s.Empleado.AreaId == currentUser.AreaId || s.SupervisorAsignadoId == currentUserId);
            }
            // Admin can see everything

            // Filtering
            if (!string.IsNullOrEmpty(request.Estado) && Enum.TryParse<EstadoSolicitud>(request.Estado, out var estadoVal))
            {
                query = query.Where(s => s.Estado == estadoVal);
            }

            if (request.TipoSolicitudId.HasValue)
            {
                query = query.Where(s => s.TipoSolicitudId == request.TipoSolicitudId.Value);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(s => s.FechaCreacion)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new SolicitudDto(
                    s.Id,
                    new TipoDto(s.TipoSolicitud.Id, s.TipoSolicitud.Nombre),
                    new EmpleadoDto(s.Empleado.Id, s.Empleado.Nombre),
                    s.SupervisorAsignado != null ? new SupervisorDto(s.SupervisorAsignado.Id, s.SupervisorAsignado.Nombre) : null,
                    s.Estado.ToString(),
                    s.FechaCreacion,
                    s.FechaEnvio,
                    s.FechaResolucion,
                    Convert.ToBase64String(s.RowVersion)
                ))
                .ToListAsync(cancellationToken);

            return new GetSolicitudesResponse(items, total, request.Page, request.PageSize);
        }
    }
}
