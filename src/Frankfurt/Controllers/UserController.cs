using Frankfurt.Models;
using Frankfurt.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Frankfurt.Controllers;


public class UserController : BaseController
{

    private readonly IOptions<AppSettings> _appSettings;
    private readonly ILogger<UserController> _logger;
    private readonly IUserService _userService;

    public UserController(IOptions<AppSettings> appSettings, ILogger<UserController> logger, IUserService userService)
    {
        _userService = userService;
        _appSettings = appSettings;
        _logger = logger;
    }


    //http://localhost:5225/api/v1/user/login
    [HttpPost]
    [Route("login")]
    [AllowAnonymous]
    public Task<IResult> Login([FromBody] UserLogin model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            
            _logger.LogWarning("Invalid login attempt: {Errors}", string.Join(", ", errors));

            return Task.FromResult<IResult>(Results.BadRequest("Invalid login request"));
        }

        var user = _userService.Login(model.Username, model.Password);
        if (user != null)
        {
            var token = _userService.GenerateToken(user);
            return Task.FromResult<IResult>(Results.Ok(new { token = token }));
        }

        return Task.FromResult<IResult>(Results.BadRequest("Invalid login request"));
    }

}