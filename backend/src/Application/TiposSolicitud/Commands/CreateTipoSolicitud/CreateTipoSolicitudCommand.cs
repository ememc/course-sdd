using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.TiposSolicitud.Commands.CreateTipoSolicitud
{
    public record CreateTipoSolicitudCommand : IRequest<Guid>
    {
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public string CamposDefinicion { get; init; } = "[]";
    }

    public class CreateTipoSolicitudCommandHandler : IRequestHandler<CreateTipoSolicitudCommand, Guid>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateTipoSolicitudCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(CreateTipoSolicitudCommand request, CancellationToken cancellationToken)
        {
            var currentUserIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(currentUserIdStr, out var adminId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            var adminUser = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == adminId && u.Activo, cancellationToken);

            if (adminUser == null || adminUser.Rol.Nombre != "Administrador")
            {
                throw new UnauthorizedAccessException("Solo los administradores pueden crear tipos de solicitud");
            }

            var nombreNormalizado = request.Nombre.Trim();
            var existe = await _context.TiposSolicitud
                .AnyAsync(t => t.Nombre.ToLower() == nombreNormalizado.ToLower(), cancellationToken);

            if (existe)
            {
                throw new InvalidOperationException("Ya existe un tipo de solicitud con este nombre");
            }

            var tipoSolicitud = new TipoSolicitud
            {
                Nombre = nombreNormalizado,
                Descripcion = request.Descripcion,
                CamposDefinicion = request.CamposDefinicion,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                CreadoPorId = adminId
            };

            _context.TiposSolicitud.Add(tipoSolicitud);
            await _context.SaveChangesAsync(cancellationToken);

            return tipoSolicitud.Id;
        }
    }
}
