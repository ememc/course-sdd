using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.TiposSolicitud.Queries.GetTipoSolicitudById
{
    public record GetTipoSolicitudByIdQuery : IRequest<TipoSolicitudDetailDto>
    {
        public Guid Id { get; init; }
    }

    public record TipoSolicitudDetailDto(
        Guid Id,
        string Nombre,
        string? Descripcion,
        string CamposDefinicion,
        bool Activo,
        DateTime FechaCreacion,
        string CreadoPorNombre
    );

    public class GetTipoSolicitudByIdQueryHandler : IRequestHandler<GetTipoSolicitudByIdQuery, TipoSolicitudDetailDto>
    {
        private readonly IAppDbContext _context;

        public GetTipoSolicitudByIdQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<TipoSolicitudDetailDto> Handle(GetTipoSolicitudByIdQuery request, CancellationToken cancellationToken)
        {
            var tipo = await _context.TiposSolicitud
                .Include(t => t.CreadoPor)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (tipo == null)
            {
                throw new KeyNotFoundException("Tipo de solicitud no encontrado");
            }

            return new TipoSolicitudDetailDto(
                tipo.Id,
                tipo.Nombre,
                tipo.Descripcion,
                tipo.CamposDefinicion,
                tipo.Activo,
                tipo.FechaCreacion,
                tipo.CreadoPor.Nombre
            );
        }
    }
}
