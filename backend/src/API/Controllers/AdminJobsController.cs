using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Application.Solicitudes.Commands.CleanExpiredDrafts;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/jobs")]
    [Authorize(Roles = "Administrador")]
    public class AdminJobsController : ControllerBase
    {
        private readonly ISender _mediator;

        public AdminJobsController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("limpiar-borradores")]
        public async Task<ActionResult> LimpiarBorradores()
        {
            await _mediator.Send(new CleanExpiredDraftsCommand());
            return Ok();
        }
    }
}
