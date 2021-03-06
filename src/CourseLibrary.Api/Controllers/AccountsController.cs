using System.Threading.Tasks;
using CourseLibrary.Application.Commands.Identity;
using CourseLibrary.Application.Queries.Identity;
using CourseLibrary.Application.Services;
using CourseLibrary.Application.Services.Identity;
using CourseLibrary.Core.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseLibrary.Api.Controllers
{
    public class AccountsController : BaseController
    {
        private readonly IAccountService _accountService;

        public AccountsController(IDispatcher dispatcher, IAccountService accountService)
            : base(dispatcher) 
                => _accountService = accountService;

        [HttpGet("{id}")]
        [Allow(Role.Admin, Role.User)]
        public async Task<IActionResult> Get([FromRoute] GetUser query) 
            => Select(await Dispatcher.QueryAsync(query));

        [HttpGet]
        [Allow(Role.Admin, Role.User)]
        public async Task<IActionResult> Get([FromRoute] GetUsers query) 
            => Select(await Dispatcher.QueryAsync(query));

        [HttpPost("sign-up")]
        [AllowAnonymous]
        public async Task<IActionResult> Post(SignUp command)
        {
            await Dispatcher.SendAsync(command);
        
            return CreatedAtAction(nameof(Get), new { Id = command.Id }, command.Id);
        }

        [HttpPost("sign-in")]
        [AllowAnonymous]
        public async Task<IActionResult> Post(SignIn command)
            => Ok(await _accountService.SignInAsync(command));

        [HttpPatch("me/change-password")]
        [Allow(Role.Admin, Role.User)]
        public async Task<IActionResult> Patch(ChangePassword command)
        {
            command.UserId = UserId;

            await Dispatcher.SendAsync(command);
        
            return NoContent();
        }
    }
}
