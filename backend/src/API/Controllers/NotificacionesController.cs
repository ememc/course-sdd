using System;
using System.Threading.Tasks;
using Application.Notificaciones.Commands.MarkAsRead;
using Application.Notificaciones.Queries.GetNotificaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class NotificacionesController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<NotificationsResponse>> GetList()
        {
            var result = await Mediator.Send(new GetNotificacionesQuery());
            return Ok(result);
        }

        [HttpPost("{id}/marcar-leida")]
        public async Task<ActionResult> MarkAsRead(Guid id)
        {
            await Mediator.Send(new MarkAsReadCommand { Id = id });
            return NoContent();
        }

        [HttpPost("marcar-todas-leidas")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            await Mediator.Send(new MarkAsReadCommand { Id = null });
            return NoContent();
        }
    }
}
