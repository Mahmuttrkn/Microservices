using Microsoft.AspNetCore.Mvc;

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