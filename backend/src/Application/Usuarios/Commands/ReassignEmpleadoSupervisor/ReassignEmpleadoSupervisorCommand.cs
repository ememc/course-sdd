using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usuarios.Commands.ReassignEmpleadoSupervisor
{
    public record ReassignEmpleadoSupervisorCommand : IRequest<Unit>
    {
        public Guid EmpleadoId { get; init; }
        public Guid? NuevoSupervisorId { get; init; }
    }

    public class ReassignEmpleadoSupervisorCommandHandler : IRequestHandler<ReassignEmpleadoSupervisorCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReassignEmpleadoSupervisorCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(ReassignEmpleadoSupervisorCommand request, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("Solo los administradores pueden reasignar el supervisor organizacional de los empleados");
            }

            var empleado = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == request.EmpleadoId && u.Activo, cancellationToken);

            if (empleado == null)
            {
                throw new KeyNotFoundException("Empleado no encontrado o inactivo");
            }

            if (empleado.Rol.Nombre != "Empleado")
            {
                throw new InvalidOperationException("El usuario seleccionado no es un empleado");
            }

            if (request.NuevoSupervisorId.HasValue)
            {
                var supervisor = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Id == request.NuevoSupervisorId.Value && u.Activo, cancellationToken);

                if (supervisor == null || supervisor.Rol.Nombre != "Supervisor")
                {
                    throw new InvalidOperationException("El supervisor seleccionado no es válido o está inactivo");
                }
            }

            empleado.SupervisorId = request.NuevoSupervisorId;
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
