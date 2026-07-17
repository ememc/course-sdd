using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usuarios.Queries.GetUsuarios
{
    public record GetUsuariosQuery : IRequest<GetUsuariosResponse>
    {
        public string? Rol { get; init; }
        public Guid? AreaId { get; init; }
        public bool? Activo { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public record GetUsuariosResponse(
        List<UsuarioDto> Data,
        int Total,
        int Page,
        int PageSize
    );

    public record UsuarioDto(
        Guid Id,
        string Nombre,
        string Email,
        string Rol,
        string Area,
        Guid? SupervisorId,
        string? SupervisorNombre,
        bool Activo,
        DateTime FechaCreacion
    );

    public class GetUsuariosQueryHandler : IRequestHandler<GetUsuariosQuery, GetUsuariosResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetUsuariosQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetUsuariosResponse> Handle(GetUsuariosQuery request, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("Solo los administradores pueden ver la lista completa de usuarios");
            }

            var query = _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Area)
                .Include(u => u.Supervisor)
                .AsNoTracking();

            // Filters
            if (!string.IsNullOrEmpty(request.Rol))
            {
                query = query.Where(u => u.Rol.Nombre.ToLower() == request.Rol.ToLower());
            }

            if (request.AreaId.HasValue)
            {
                query = query.Where(u => u.AreaId == request.AreaId.Value);
            }

            if (request.Activo.HasValue)
            {
                query = query.Where(u => u.Activo == request.Activo.Value);
            }

            var total = await query.CountAsync(cancellationToken);

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

            var items = await query
                .OrderBy(u => u.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UsuarioDto(
                    u.Id,
                    u.Nombre,
                    u.Email,
                    u.Rol.Nombre,
                    u.Area.Nombre,
                    u.SupervisorId,
                    u.Supervisor != null ? u.Supervisor.Nombre : null,
                    u.Activo,
                    u.FechaCreacion
                ))
                .ToListAsync(cancellationToken);

            return new GetUsuariosResponse(items, total, page, pageSize);
        }
    }
}
