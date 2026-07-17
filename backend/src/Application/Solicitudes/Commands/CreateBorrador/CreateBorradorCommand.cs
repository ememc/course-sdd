using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Solicitudes.Commands.CreateBorrador
{
    public record CreateBorradorCommand : IRequest<CreateBorradorResponse>
    {
        public Guid TipoSolicitudId { get; init; }
    }

    public record CreateBorradorResponse(
        Guid Id,
        string Estado,
        DateTime FechaCreacion,
        string RowVersion
    );

    public class CreateBorradorCommandHandler : IRequestHandler<CreateBorradorCommand, CreateBorradorResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateBorradorCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CreateBorradorResponse> Handle(CreateBorradorCommand request, CancellationToken cancellationToken)
        {
            var userIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(userIdStr, out var empleadoId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            var tipoSolicitud = await _context.TiposSolicitud
                .FirstOrDefaultAsync(t => t.Id == request.TipoSolicitudId && t.Activo, cancellationToken);

            if (tipoSolicitud == null)
            {
                throw new InvalidOperationException("Tipo de solicitud no válido o inactivo");
            }

            // Get supervisor organizational id of current employee
            var empleado = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == empleadoId && u.Activo, cancellationToken);

            if (empleado == null)
            {
                throw new InvalidOperationException("Empleado no encontrado o inactivo");
            }

            var solicitud = new Solicitud
            {
                TipoSolicitudId = request.TipoSolicitudId,
                EmpleadoId = empleadoId,
                SupervisorAsignadoId = empleado.SupervisorId,
                Estado = EstadoSolicitud.Borrador,
                CamposDinamicos = "{}"
            };

            _context.Solicitudes.Add(solicitud);
            await _context.SaveChangesAsync(cancellationToken);

            return new CreateBorradorResponse(
                solicitud.Id,
                solicitud.Estado.ToString(),
                solicitud.FechaCreacion,
                Convert.ToBase64String(solicitud.RowVersion)
            );
        }
    }
}
