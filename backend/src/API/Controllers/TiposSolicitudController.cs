using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.TiposSolicitud.Commands.CreateTipoSolicitud;
using Application.TiposSolicitud.Commands.DeleteTipoSolicitud;
using Application.TiposSolicitud.Commands.ToggleTipoSolicitud;
using Application.TiposSolicitud.Commands.UpdateTipoSolicitud;
using Application.TiposSolicitud.Queries.GetTipoSolicitudById;
using Application.TiposSolicitud.Queries.GetTiposSolicitud;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class TiposSolicitudController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<TipoSolicitudDto>>> GetList([FromQuery] bool? activo)
        {
            var result = await Mediator.Send(new GetTiposSolicitudQuery { Activo = activo });
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TipoSolicitudDetailDto>> GetById(Guid id)
        {
            var result = await Mediator.Send(new GetTipoSolicitudByIdQuery { Id = id });
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateTipoSolicitudCommand command)
        {
            var id = await Mediator.Send(command);
            return Ok(id);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateTipoSolicitudCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("El ID especificado en la ruta no coincide con el del cuerpo.");
            }

            await Mediator.Send(command);
            return NoContent();
        }

        [HttpPost("{id}/toggle")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult> Toggle(Guid id, [FromBody] ToggleTipoSolicitudCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("El ID especificado en la ruta no coincide con el del cuerpo.");
            }

            await Mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await Mediator.Send(new DeleteTipoSolicitudCommand { Id = id });
            return NoContent();
        }
    }
}
