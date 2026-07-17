using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Usuarios.Commands.DeactivateUsuario;
using Application.Usuarios.Commands.ReassignEmpleadoSupervisor;
using Application.Usuarios.Queries.GetSupervisores;
using Application.Usuarios.Queries.GetUsuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<GetUsuariosResponse>> GetList([FromQuery] GetUsuariosQuery query)
        {
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("supervisores")]
        [AllowAnonymous]
        public async Task<ActionResult<List<SupervisorDto>>> GetSupervisores([FromQuery] bool? soloActivos)
        {
            var result = await Mediator.Send(new GetSupervisoresQuery { SoloActivos = soloActivos });
            return Ok(result);
        }

        [HttpPatch("{id}/supervisor")]
        public async Task<ActionResult> ReassignSupervisor(Guid id, [FromBody] ReassignSupervisorDto dto)
        {
            await Mediator.Send(new ReassignEmpleadoSupervisorCommand
            {
                EmpleadoId = id,
                NuevoSupervisorId = dto.NuevoSupervisorId
            });
            return Ok();
        }

        [HttpPost("{id}/desactivar")]
        public async Task<ActionResult> Deactivate(Guid id)
        {
            var result = await Mediator.Send(new DeactivateUsuarioCommand { UsuarioId = id, Activo = false });
            if (!result.Success)
            {
                return Conflict(new
                {
                    title = "El supervisor tiene solicitudes activas pendientes.",
                    status = 409,
                    solicitudesPendientes = result.PendingSolicitudes
                });
            }
            return NoContent();
        }
    }

    public class ReassignSupervisorDto
    {
        public Guid? NuevoSupervisorId { get; set; }
    }
}
