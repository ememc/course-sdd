using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            // Apply migrations automatically
            await context.Database.MigrateAsync(cancellationToken);

            // Seed Area
            var areaId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
            if (!await context.Areas.AnyAsync(cancellationToken))
            {
                var generalArea = new Area
                {
                    Id = areaId,
                    Nombre = "General",
                    Descripcion = "Área general organizacional"
                };
                context.Areas.Add(generalArea);
                await context.SaveChangesAsync(cancellationToken);
            }

            // Seed Roles
            var rolEmpleadoId = Guid.Parse("r1111111-1111-1111-1111-111111111111");
            var rolSupervisorId = Guid.Parse("r2222222-2222-2222-2222-222222222222");
            var rolAdminId = Guid.Parse("r3333333-3333-3333-3333-333333333333");

            if (!await context.Roles.AnyAsync(cancellationToken))
            {
                context.Roles.AddRange(
                    new Rol { Id = rolEmpleadoId, Nombre = "Empleado", Descripcion = "Rol de Empleado" },
                    new Rol { Id = rolSupervisorId, Nombre = "Supervisor", Descripcion = "Rol de Supervisor" },
                    new Rol { Id = rolAdminId, Nombre = "Administrador", Descripcion = "Rol de Administrador" }
                );
                await context.SaveChangesAsync(cancellationToken);
            }

            // Seed Users
            if (!await context.Usuarios.AnyAsync(cancellationToken))
            {
                var admin = new Usuario
                {
                    Id = Guid.Parse("u1111111-1111-1111-1111-111111111111"),
                    Nombre = "Admin Test",
                    Email = "admin@test.com",
                    PasswordHash = PasswordHasher.HashPassword("Admin@123!"),
                    RolId = rolAdminId,
                    AreaId = areaId,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var supervisor1 = new Usuario
                {
                    Id = Guid.Parse("u2222222-2222-2222-2222-222222222222"),
                    Nombre = "Supervisor 1",
                    Email = "supervisor1@test.com",
                    PasswordHash = PasswordHasher.HashPassword("Sup@123!"),
                    RolId = rolSupervisorId,
                    AreaId = areaId,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var supervisor2 = new Usuario
                {
                    Id = Guid.Parse("u3333333-3333-3333-3333-333333333333"),
                    Nombre = "Supervisor 2",
                    Email = "supervisor2@test.com",
                    PasswordHash = PasswordHasher.HashPassword("Sup@123!"),
                    RolId = rolSupervisorId,
                    AreaId = areaId,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var empleado1 = new Usuario
                {
                    Id = Guid.Parse("u4444444-4444-4444-4444-444444444444"),
                    Nombre = "Empleado 1",
                    Email = "empleado1@test.com",
                    PasswordHash = PasswordHasher.HashPassword("Emp@123!"),
                    RolId = rolEmpleadoId,
                    AreaId = areaId,
                    SupervisorId = supervisor1.Id,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var empleado2 = new Usuario
                {
                    Id = Guid.Parse("u5555555-5555-5555-5555-555555555555"),
                    Nombre = "Empleado 2",
                    Email = "empleado2@test.com",
                    PasswordHash = PasswordHasher.HashPassword("Emp@123!"),
                    RolId = rolEmpleadoId,
                    AreaId = areaId,
                    SupervisorId = supervisor1.Id,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var empleado3 = new Usuario
                {
                    Id = Guid.Parse("u6666666-6666-6666-6666-666666666666"),
                    Nombre = "Empleado 3",
                    Email = "empleado3@test.com",
                    PasswordHash = PasswordHasher.HashPassword("Emp@123!"),
                    RolId = rolEmpleadoId,
                    AreaId = areaId,
                    SupervisorId = supervisor2.Id,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                context.Usuarios.AddRange(admin, supervisor1, supervisor2, empleado1, empleado2, empleado3);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
