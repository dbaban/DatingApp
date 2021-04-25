using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTO;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext dataContext;
        private readonly ITokenService tokenService;
        public AccountController(DataContext dataContext, ITokenService tokenService)
        {
            this.tokenService = tokenService;
            this.dataContext = dataContext;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExist(registerDto.Username)) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();

            var user = new AppUser
            {
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            this.dataContext.Users.Add(user);
            await this.dataContext.SaveChangesAsync();
            return new UserDto
            {
                UserName=user.UserName,
                Token=tokenService.CreateToken(user)
            };

        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await dataContext.Users.SingleOrDefaultAsync(p => p.UserName == loginDto.Username);

            if (user == null) return Unauthorized("Invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }

             return new UserDto
            {
                UserName=user.UserName,
                Token=tokenService.CreateToken(user)
            };
        }
        private async Task<bool> UserExist(string userName)
        {
            return await this.dataContext.Users.AnyAsync(x => x.UserName == userName.ToLower());
        }

    }
}