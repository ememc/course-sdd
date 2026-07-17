using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [AllowAnonymous]
    public class AuthController : ApiControllerBase
    {
        private readonly IAppDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthController(IAppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public record LoginRequest(string Email, string Password);

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Title = "El correo electrónico y la contraseña son requeridos" });
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Area)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo, cancellationToken);

            if (usuario == null)
            {
                return Unauthorized(new { Title = "Credenciales incorrectas o usuario inactivo" });
            }

            var passwordValid = PasswordHasher.VerifyPassword(request.Password, usuario.PasswordHash);
            if (!passwordValid)
            {
                return Unauthorized(new { Title = "Credenciales incorrectas o usuario inactivo" });
            }

            var token = _tokenService.GenerateJwtToken(usuario);

            return Ok(new
            {
                AccessToken = token,
                User = new
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Email = usuario.Email,
                    Rol = usuario.Rol.Nombre,
                    AreaId = usuario.AreaId,
                    AreaNombre = usuario.Area.Nombre
                }
            });
        }
    }
}
