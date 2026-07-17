using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usuarios.Queries.GetSupervisores
{
    public record GetSupervisoresQuery : IRequest<List<SupervisorDto>>
    {
        public bool? SoloActivos { get; init; } = true;
    }

    public record SupervisorDto(
        Guid Id,
        string Nombre,
        string Email,
        bool Activo,
        int ActiveRequestsCount
    );

    public class GetSupervisoresQueryHandler : IRequestHandler<GetSupervisoresQuery, List<SupervisorDto>>
    {
        private readonly IAppDbContext _context;

        public GetSupervisoresQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SupervisorDto>> Handle(GetSupervisoresQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol.Nombre == "Supervisor");

            if (request.SoloActivos.HasValue && request.SoloActivos.Value)
            {
                query = query.Where(u => u.Activo);
            }

            var supervisors = await query
                .OrderBy(u => u.Nombre)
                .ToListAsync(cancellationToken);

            var result = new List<SupervisorDto>();

            foreach (var sup in supervisors)
            {
                var activeRequestsCount = await _context.Solicitudes
                    .CountAsync(s => s.SupervisorAsignadoId == sup.Id &&
                                    (s.Estado == EstadoSolicitud.Enviada || s.Estado == EstadoSolicitud.EnRevision),
                                cancellationToken);

                result.Add(new SupervisorDto(
                    sup.Id,
                    sup.Nombre,
                    sup.Email,
                    sup.Activo,
                    activeRequestsCount
                ));
            }

            return result;
        }
    }
}
