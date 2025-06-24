using crud_api.models;
using crud_api.Services;
using crud_api.Dto;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using crud_api.common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace crud_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(UserService userService,IMapper mapper,JwtService jwtService) : ControllerBase
    {
        private readonly UserService _userService = userService;
        private readonly JwtService _jwtservice = jwtService;
        private readonly IMapper _mapper = mapper;


        [AllowAnonymous]
        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            var result = _jwtservice.Authenticate(request);
            if(result is null) return Unauthorized();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("logout")]
        public ActionResult<LoginResponse> LogOut()
        {
            var user = _userService.GetUserById(Int32.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!));
            if (user is null)
            {
                return Unauthorized();
            }
            user.Token = null;
            _userService.UpdateUser(user);
            return Ok(new {message="Log out Success"});
        }


        [HttpGet("getall")]
        public ActionResult<List<UserDto>> GetUsers(){
            return Ok(_mapper.Map<List<UserDto>>(_userService.GetAllUsers()));
        }
        [Authorize]
        [HttpGet("get")]
        public ActionResult<UserDto> GetUserById()
        {
            var user = _userService.GetUserById(Int32.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!));
            if (user is null)
            {
                BadRequest(new {
                error = "User Not Found!"
                });
            }
            return Ok(_mapper.Map<UserDto>(user));
        }

        [AllowAnonymous]
        [HttpPost("create")]
        public ActionResult<UserDto> AddUser([FromBody] CreateUserDto user)
        {
            if (user is null)
            {
                return BadRequest();
            }

            User? newUser = _userService.CreateUser(user);
            if (newUser is null){
                return Ok("User Email Already Exists");
            }
            var loginRequest = new LoginRequest { Email = newUser.Email, Password = user.Password };
            LoginResponse credentials = _jwtservice.Authenticate(loginRequest)!;
            newUser.Token = credentials.AccessToken;
            _userService.UpdateUser(newUser);
            var userDto = _mapper.Map<UserDto>(newUser);
            userDto.Credentials = credentials;
            return CreatedAtAction(nameof(GetUserById), new { Id = newUser.Id },userDto);
        }

        [Authorize]
        [HttpPut("update")]
        public ActionResult<UserDto> UpdateUser(UpdateUserDto updatedUser){
            var user = _userService.GetUserById(Int32.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!));
            if (user is null)  return BadRequest(new {
                error = "User Not Found!"
            });
            if (_userService.EmailExists(updatedUser.Email)) return BadRequest(new {
                error = "Email Already exists"
            });
            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            if (!string.IsNullOrEmpty(updatedUser.Password))
            {
                user.Password = Utilities.ComputeSHA256(updatedUser.Password);
            }
            return Ok(_mapper.Map<UserDto>(_userService.UpdateUser(user)));
        }

        [Authorize]
        [HttpDelete("delete")]
        public IActionResult DeleteUserbyId(){
            _userService.DeleteUser(Int32.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!));
            return NoContent();
        }
    }
}