using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Notificaciones.Queries.GetNotificaciones
{
    public record GetNotificacionesQuery : IRequest<NotificationsResponse>
    {
    }

    public record NotificationsResponse(
        List<NotificacionDto> Data,
        int NoLeidas
    );

    public record NotificacionDto(
        Guid Id,
        string Tipo,
        string Contenido,
        bool Leida,
        DateTime FechaGeneracion,
        SolicitudRelationDto? Solicitud
    );

    public record SolicitudRelationDto(Guid Id);

    public class GetNotificacionesQueryHandler : IRequestHandler<GetNotificacionesQuery, NotificationsResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetNotificacionesQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<NotificationsResponse> Handle(GetNotificacionesQuery request, CancellationToken cancellationToken)
        {
            var userIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            // Retrieve all notifications for the user, top 50, sorted by creation date descending
            var notifications = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.UsuarioId == userId)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(50)
                .ToListAsync(cancellationToken);

            var unreadCount = await _context.Notificaciones
                .CountAsync(n => n.UsuarioId == userId && !n.Leido, cancellationToken);

            var dataList = notifications.Select(n => {
                // Determine tipo icon keyword for frontend
                string tipo = "Notificacion";
                if (n.Mensaje.Contains("enviado") || n.Mensaje.Contains("nueva")) tipo = "NuevaSolicitud";
                else if (n.Mensaje.Contains("aprobada")) tipo = "SolicitudAprobada";
                else if (n.Mensaje.Contains("rechazada")) tipo = "SolicitudRechazada";
                else if (n.Mensaje.Contains("reasignado") || n.Mensaje.Contains("asignada")) tipo = "SolicitudReasignada";

                return new NotificacionDto(
                    n.Id,
                    tipo,
                    n.Mensaje,
                    n.Leido, // true means read, matches frontend "leida: true"
                    n.FechaCreacion,
                    n.SolicitudId.HasValue ? new SolicitudRelationDto(n.SolicitudId.Value) : null
                );
            }).ToList();

            return new NotificationsResponse(dataList, unreadCount);
        }
    }
}
