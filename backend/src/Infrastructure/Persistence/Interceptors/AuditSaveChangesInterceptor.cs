using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using System;
using Domain.Entities;
using Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Persistence.Interceptors
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;

        public AuditSaveChangesInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void UpdateEntities(DbContext? context)
        {
            if (context == null) return;

            var currentUserIdStr = _currentUserService.UserId;
            var currentUserId = Guid.TryParse(currentUserIdStr, out var userId) ? userId : Guid.Empty;

            var auditEvents = new List<EventoAuditoria>();

            foreach (var entry in context.ChangeTracker.Entries<Solicitud>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.FechaCreacion = DateTime.UtcNow;
                    entry.Entity.UltimaModificacion = DateTime.UtcNow;

                    auditEvents.Add(new EventoAuditoria
                    {
                        Solicitud = entry.Entity,
                        UsuarioId = currentUserId != Guid.Empty ? currentUserId : entry.Entity.EmpleadoId,
                        EstadoAnterior = null,
                        EstadoNuevo = entry.Entity.Estado.ToString(),
                        Accion = "Creada",
                        FechaHora = DateTime.UtcNow
                    });
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UltimaModificacion = DateTime.UtcNow;

                    var estadoProp = entry.Property(x => x.Estado);
                    var supervisorProp = entry.Property(x => x.SupervisorAsignadoId);

                    if (estadoProp.IsModified)
                    {
                        var estadoAnterior = estadoProp.OriginalValue;
                        var estadoNuevo = estadoProp.CurrentValue;

                        string accion = estadoNuevo switch
                        {
                            EstadoSolicitud.Enviada => "Enviada",
                            EstadoSolicitud.EnRevision => "Tomada",
                            EstadoSolicitud.Aprobada => "Aprobada",
                            EstadoSolicitud.Rechazada => "Rechazada",
                            EstadoSolicitud.Cancelada => "Cancelada",
                            _ => "Modificada"
                        };

                        auditEvents.Add(new EventoAuditoria
                        {
                            Solicitud = entry.Entity,
                            UsuarioId = currentUserId,
                            EstadoAnterior = estadoAnterior.ToString(),
                            EstadoNuevo = estadoNuevo.ToString(),
                            Accion = accion,
                            FechaHora = DateTime.UtcNow
                        });
                    }
                    else if (supervisorProp.IsModified)
                    {
                        auditEvents.Add(new EventoAuditoria
                        {
                            Solicitud = entry.Entity,
                            UsuarioId = currentUserId,
                            EstadoAnterior = entry.Entity.Estado.ToString(),
                            EstadoNuevo = entry.Entity.Estado.ToString(),
                            Accion = "Reasignada",
                            FechaHora = DateTime.UtcNow,
                            Metadata = $"{{\"supervisorAnterior\":\"{supervisorProp.OriginalValue}\",\"supervisorNuevo\":\"{supervisorProp.CurrentValue}\"}}"
                        });
                    }
                }
            }

            if (auditEvents.Any())
            {
                context.Set<EventoAuditoria>().AddRange(auditEvents);
            }
        }
    }
}
