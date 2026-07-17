using Application.Solicitudes.Commands.CreateBorrador;
using Application.Solicitudes.Commands.UpdateBorrador;
using Application.Solicitudes.Commands.SubmitSolicitud;
using Application.Solicitudes.Commands.CancelSolicitud;
using Application.Solicitudes.Commands.ReassignSolicitudes;
using Application.Solicitudes.Commands.TomarSolicitud;
using Application.Solicitudes.Commands.AprobarSolicitud;
using Application.Solicitudes.Commands.RechazarSolicitud;
using Application.Solicitudes.Queries.GetSolicitudes;
using Application.Solicitudes.Queries.GetSolicitudById;
using Application.Solicitudes.Queries.GetAuditoria;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Authorize]
    public class SolicitudesController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<GetSolicitudesResponse>> GetList([FromQuery] GetSolicitudesQuery query)
        {
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SolicitudDetailDto>> GetById(Guid id)
        {
            var result = await Mediator.Send(new GetSolicitudByIdQuery { Id = id });
            return Ok(result);
        }

        [HttpGet("{id}/auditoria")]
        public async Task<ActionResult<GetAuditoriaResponse>> GetAuditoria(Guid id)
        {
            var result = await Mediator.Send(new GetAuditoriaQuery { SolicitudId = id });
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<CreateBorradorResponse>> Create(CreateBorradorCommand command)
        {
            var result = await Mediator.Send(command);
            return StatusCode(201, result);
        }

        [HttpPatch("{id}/borrador")]
        public async Task<ActionResult<UpdateBorradorResponse>> UpdateBorrador(Guid id, UpdateBorradorDto dto)
        {
            var rowVersion = GetRowVersionFromHeader();
            if (string.IsNullOrEmpty(rowVersion))
            {
                return BadRequest("El encabezado 'If-Match' es obligatorio para esta operación.");
            }

            var command = new UpdateBorradorCommand
            {
                Id = id,
                CamposDinamicos = dto.CamposDinamicos,
                RowVersion = rowVersion
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("{id}/enviar")]
        public async Task<ActionResult<SubmitSolicitudResponse>> Enviar(Guid id)
        {
            var rowVersion = GetRowVersionFromHeader();
            if (string.IsNullOrEmpty(rowVersion))
            {
                return BadRequest("El encabezado 'If-Match' es obligatorio para esta operación.");
            }

            var command = new SubmitSolicitudCommand
            {
                Id = id,
                RowVersion = rowVersion
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("{id}/cancelar")]
        public async Task<ActionResult<CancelSolicitudResponse>> Cancelar(Guid id)
        {
            var command = new CancelSolicitudCommand { Id = id };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("{id}/tomar")]
        public async Task<ActionResult<Application.Solicitudes.Commands.TomarSolicitud.TomarSolicitudResponse>> Tomar(Guid id)
        {
            var rowVersion = GetRowVersionFromHeader();
            if (string.IsNullOrEmpty(rowVersion))
            {
                return BadRequest("El encabezado 'If-Match' es obligatorio para esta operación.");
            }

            var command = new Application.Solicitudes.Commands.TomarSolicitud.TomarSolicitudCommand
            {
                Id = id,
                RowVersion = rowVersion
            };

            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("{id}/aprobar")]
        public async Task<ActionResult<Application.Solicitudes.Commands.AprobarSolicitud.AprobarSolicitudResponse>> Aprobar(Guid id)
        {
            var command = new Application.Solicitudes.Commands.AprobarSolicitud.AprobarSolicitudCommand { Id = id };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("{id}/rechazar")]
        public async Task<ActionResult<Application.Solicitudes.Commands.RechazarSolicitud.RechazarSolicitudResponse>> Rechazar(Guid id, RechazarDto dto)
        {
            var command = new Application.Solicitudes.Commands.RechazarSolicitud.RechazarSolicitudCommand
            {
                Id = id,
                Comentario = dto.Comentario
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("reasignar")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult> Reassign([FromBody] ReassignSolicitudesCommand command)
        {
            await Mediator.Send(command);
            return Ok(new
            {
                reasignadas = command.SolicitudIds.Count,
                errores = new List<string>()
            });
        }

        private string GetRowVersionFromHeader()
        {
            if (Request.Headers.TryGetValue("If-Match", out var ifMatchValues))
            {
                return ifMatchValues.ToString().Trim('\"');
            }
            return string.Empty;
        }
    }

    public class UpdateBorradorDto
    {
        public Dictionary<string, object> CamposDinamicos { get; set; } = new();
    }

    public class RechazarDto
    {
        public string Comentario { get; set; } = string.Empty;
    }
}
