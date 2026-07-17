using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;

namespace Application.Solicitudes.Commands.CleanExpiredDrafts
{
    public record CleanExpiredDraftsCommand : IRequest<Unit>;

    public class CleanExpiredDraftsCommandHandler : IRequestHandler<CleanExpiredDraftsCommand, Unit>
    {
        private readonly IAppDbContext _context;

        public CleanExpiredDraftsCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(CleanExpiredDraftsCommand request, CancellationToken cancellationToken)
        {
            var expirationThreshold = DateTime.UtcNow.AddDays(-3);

            var expiredDrafts = await _context.Solicitudes
                .Where(s => s.Estado == EstadoSolicitud.Borrador && s.FechaCreacion < expirationThreshold)
                .ToListAsync(cancellationToken);

            if (expiredDrafts.Count > 0)
            {
                _context.Solicitudes.RemoveRange(expiredDrafts);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Unit.Value;
        }
    }
}
