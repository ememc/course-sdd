using MediatR;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Solicitudes.Commands.UpdateBorrador
{
    public record UpdateBorradorCommand : IRequest<UpdateBorradorResponse>
    {
        public Guid Id { get; init; }
        public Dictionary<string, object> CamposDinamicos { get; init; } = new();
        public string RowVersion { get; init; } = string.Empty;
    }

    public record UpdateBorradorResponse(
        DateTime UltimaModificacion,
        string RowVersion
    );

    public class UpdateBorradorCommandHandler : IRequestHandler<UpdateBorradorCommand, UpdateBorradorResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateBorradorCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<UpdateBorradorResponse> Handle(UpdateBorradorCommand request, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("No tiene permisos para modificar esta solicitud");
            }

            if (solicitud.Estado != EstadoSolicitud.Borrador)
            {
                throw new InvalidOperationException("Solo se pueden modificar solicitudes en estado Borrador");
            }

            // Map RowVersion for concurrency token check
            var dbContext = (_context as DbContext);
            if (dbContext != null)
            {
                dbContext.Entry(solicitud).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
            }

            solicitud.CamposDinamicos = JsonSerializer.Serialize(request.CamposDinamicos);
            solicitud.UltimaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new UpdateBorradorResponse(
                solicitud.UltimaModificacion,
                Convert.ToBase64String(solicitud.RowVersion)
            );
        }
    }
}
