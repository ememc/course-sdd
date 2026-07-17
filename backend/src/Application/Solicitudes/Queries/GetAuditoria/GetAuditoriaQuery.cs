using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Solicitudes.Queries.GetAuditoria
{
    public record GetAuditoriaQuery : IRequest<GetAuditoriaResponse>
    {
        public Guid SolicitudId { get; init; }
    }

    public record GetAuditoriaResponse(List<EventoAuditoriaDto> Data);

    public record EventoAuditoriaDto(
        Guid Id,
        UsuarioAuditoriaDto Usuario,
        string? EstadoAnterior,
        string EstadoNuevo,
        string Accion,
        DateTime FechaHora,
        object? Metadata
    );

    public record UsuarioAuditoriaDto(Guid Id, string Nombre);

    public class GetAuditoriaQueryHandler : IRequestHandler<GetAuditoriaQuery, GetAuditoriaResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAuditoriaQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetAuditoriaResponse> Handle(GetAuditoriaQuery request, CancellationToken cancellationToken)
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
                .Include(s => s.Empleado)
                .FirstOrDefaultAsync(s => s.Id == request.SolicitudId, cancellationToken);

            if (solicitud == null)
            {
                throw new KeyNotFoundException("Solicitud no encontrada");
            }

            // Role-based visibility check:
            // "Solo empleado dueño, supervisor asignado o admin."
            if (currentUser.Rol.Nombre == "Empleado" && solicitud.EmpleadoId != currentUserId)
            {
                throw new KeyNotFoundException("Solicitud no encontrada");
            }
            else if (currentUser.Rol.Nombre == "Supervisor" &&
                     solicitud.Empleado.AreaId != currentUser.AreaId &&
                     solicitud.SupervisorAsignadoId != currentUserId)
            {
                throw new KeyNotFoundException("Solicitud no encontrada");
            }

            var eventos = await _context.EventosAuditoria
                .Include(e => e.Usuario)
                .Where(e => e.SolicitudId == request.SolicitudId)
                .OrderBy(e => e.FechaHora)
                .ToListAsync(cancellationToken);

            var data = eventos.Select(e =>
            {
                object? parsedMetadata = null;
                if (!string.IsNullOrEmpty(e.Metadata))
                {
                    try
                    {
                        parsedMetadata = JsonSerializer.Deserialize<Dictionary<string, object>>(e.Metadata);
                    }
                    catch
                    {
                        // Fallback to raw string if not valid dictionary JSON
                        parsedMetadata = e.Metadata;
                    }
                }

                return new EventoAuditoriaDto(
                    e.Id,
                    new UsuarioAuditoriaDto(e.Usuario.Id, e.Usuario.Nombre),
                    e.EstadoAnterior,
                    e.EstadoNuevo,
                    e.Accion,
                    e.FechaHora,
                    parsedMetadata
                );
            }).ToList();

            return new GetAuditoriaResponse(data);
        }
    }
}
