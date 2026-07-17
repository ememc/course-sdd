using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.TiposSolicitud.Queries.GetTiposSolicitud
{
    public record GetTiposSolicitudQuery : IRequest<List<TipoSolicitudDto>>
    {
        public bool? Activo { get; init; }
    }

    public record TipoSolicitudDto(
        Guid Id,
        string Nombre,
        string? Descripcion,
        string CamposDefinicion,
        bool Activo,
        DateTime FechaCreacion
    );

    public class GetTiposSolicitudQueryHandler : IRequestHandler<GetTiposSolicitudQuery, List<TipoSolicitudDto>>
    {
        private readonly IAppDbContext _context;

        public GetTiposSolicitudQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TipoSolicitudDto>> Handle(GetTiposSolicitudQuery request, CancellationToken cancellationToken)
        {
            var query = _context.TiposSolicitud.AsNoTracking();

            if (request.Activo.HasValue)
            {
                query = query.Where(t => t.Activo == request.Activo.Value);
            }

            return await query
                .OrderBy(t => t.Nombre)
                .Select(t => new TipoSolicitudDto(
                    t.Id,
                    t.Nombre,
                    t.Descripcion,
                    t.CamposDefinicion,
                    t.Activo,
                    t.FechaCreacion
                ))
                .ToListAsync(cancellationToken);
        }
    }
}
