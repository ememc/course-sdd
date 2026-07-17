using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Notificaciones.Commands.MarkAsRead
{
    public record MarkAsReadCommand : IRequest<Unit>
    {
        public Guid? Id { get; init; }
    }

    public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MarkAsReadCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
        {
            var userIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            if (request.Id.HasValue)
            {
                // Mark single notification as read
                var notif = await _context.Notificaciones
                    .FirstOrDefaultAsync(n => n.Id == request.Id.Value && n.UsuarioId == userId, cancellationToken);

                if (notif != null)
                {
                    notif.Leido = true;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                // Mark all as read
                var unread = await _context.Notificaciones
                    .Where(n => n.UsuarioId == userId && !n.Leido)
                    .ToListAsync(cancellationToken);

                foreach (var notif in unread)
                {
                    notif.Leido = true;
                }

                if (unread.Any())
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return Unit.Value;
        }
    }
}
