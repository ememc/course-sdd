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
using FluentValidation;

namespace Application.Solicitudes.Commands.SubmitSolicitud
{
    public record SubmitSolicitudCommand : IRequest<SubmitSolicitudResponse>
    {
        public Guid Id { get; init; }
        public string RowVersion { get; init; } = string.Empty;
    }

    public record SubmitSolicitudResponse(
        Guid Id,
        string Estado,
        DateTime FechaEnvio
    );

    public class SubmitSolicitudCommandHandler : IRequestHandler<SubmitSolicitudCommand, SubmitSolicitudResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SubmitSolicitudCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<SubmitSolicitudResponse> Handle(SubmitSolicitudCommand request, CancellationToken cancellationToken)
        {
            var userIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(userIdStr, out var empleadoId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            var solicitud = await _context.Solicitudes
                .Include(s => s.TipoSolicitud)
                .Include(s => s.Empleado)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (solicitud == null)
            {
                throw new KeyNotFoundException("Solicitud no encontrada");
            }

            if (solicitud.EmpleadoId != empleadoId)
            {
                throw new UnauthorizedAccessException("No tiene permisos para enviar esta solicitud");
            }

            if (solicitud.Estado != EstadoSolicitud.Borrador)
            {
                throw new InvalidOperationException("Solo se pueden enviar solicitudes en estado Borrador");
            }

            // Concurrency check
            var dbContext = (_context as DbContext);
            if (dbContext != null)
            {
                dbContext.Entry(solicitud).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
            }

            // Validate Dynamic Fields
            ValidateDynamicFields(solicitud);

            solicitud.Estado = EstadoSolicitud.Enviada;
            solicitud.FechaEnvio = DateTime.UtcNow;

            // Create notification for supervisor
            if (solicitud.SupervisorAsignadoId.HasValue)
            {
                var notificacion = new Notificacion
                {
                    UsuarioId = solicitud.SupervisorAsignadoId.Value,
                    Mensaje = $"El empleado {solicitud.Empleado.Nombre} ha enviado una nueva solicitud de tipo {solicitud.TipoSolicitud.Nombre}.",
                    Leido = false,
                    FechaCreacion = DateTime.UtcNow,
                    SolicitudId = solicitud.Id
                };
                _context.Notificaciones.Add(notificacion);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new SubmitSolicitudResponse(
                solicitud.Id,
                solicitud.Estado.ToString(),
                solicitud.FechaEnvio.Value
            );
        }

        private void ValidateDynamicFields(Solicitud solicitud)
        {
            if (string.IsNullOrEmpty(solicitud.TipoSolicitud.CamposDefinicion)) return;

            var definitionJson = JsonDocument.Parse(solicitud.TipoSolicitud.CamposDefinicion);
            var valuesJson = JsonDocument.Parse(solicitud.CamposDinamicos);

            var validationFailures = new List<FluentValidation.Results.ValidationFailure>();

            foreach (var element in definitionJson.RootElement.EnumerateArray())
            {
                var nombre = element.GetProperty("nombre").GetString() ?? string.Empty;
                var tipo = element.GetProperty("tipo").GetString() ?? string.Empty;
                var requerido = element.TryGetProperty("requerido", out var reqProp) && reqProp.GetBoolean();

                var hasValue = valuesJson.RootElement.TryGetProperty(nombre, out var valueProp);
                var valueStr = hasValue ? valueProp.ToString() : null;

                if (requerido && (string.IsNullOrEmpty(valueStr) || string.IsNullOrWhiteSpace(valueStr)))
                {
                    validationFailures.Add(new FluentValidation.Results.ValidationFailure($"camposDinamicos.{nombre}", $"El campo '{nombre}' es requerido."));
                }
            }

            if (validationFailures.Count > 0)
            {
                throw new ValidationException(validationFailures);
            }
        }
    }
}
