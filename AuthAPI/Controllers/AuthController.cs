using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        IConfiguration _configuration;
        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]
        public IActionResult Login(string userName, string password)
        {
            TokenHandler._configuration = _configuration;
            return Ok(userName == "mhmt" && password == "12345" ? TokenHandler.CreateAccessToken() : new UnauthorizedResult());
        }
    }
}
