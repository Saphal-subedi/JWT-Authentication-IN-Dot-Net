using JwtAuthDotNet9.Entities;
using JwtAuthDotNet9.Models;
using JwtAuthDotNet9.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JwtAuthDotNet9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService,IConfiguration configuration) : ControllerBase
    {
        public static User user = new();
        [HttpPost("register")]
        ////This method returns user and hashed password and while working we save hashed value in database and while authenticating we compare database and its hashed value.
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            //var hashedPassword = new PasswordHasher<User>().HashPassword(user, request.Password);
            //user.Username = request.Username;
            //user.PasswordHash = hashedPassword;
            //return Ok(user);
            var user =await authService.RegisterAsync(request);
            if(user is null)
            {
                return BadRequest("User already exists.");
            }   
            return Ok(user);

        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDto request)
        {
            //if(user.Username!=request.Username)
            //{
            //    return BadRequest("User not found");
            //}
            //if(new PasswordHasher<User>().VerifyHashedPassword(user,user.PasswordHash,request.Password)==PasswordVerificationResult.Failed )
            //{
            //    return BadRequest("Wrong password.");
            //}
            //string token = CreateToken(user);
            //return Ok(token);

            var token = await authService.LoginAsync(request);
            if(token is null)
            {
                return BadRequest("Invalid username or password.");
            }
            return Ok(token);
        }
        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                ////Here this time we have name so we are using name here otherwise we will be using ID here
                new Claim(ClaimTypes.Name,user.Username)
            };

            ////For Adding this methods we need to add   System.IdentityModel.Tokens.Jwt package from Nuget Package Manager 
            ////This configuration value is taken from appsettings.json file
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));
            ////When we are using HmacSha512Signature we need to take 512 bit or 64 byte key length from configuration
            /////This will be the signing credentials which will be used while creating token
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new JwtSecurityToken(
                ////Who is actually issuing the token
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
               ////who are the user use is using this token
               audience: configuration.GetValue<string>("AppSettings:Audience"),
               claims: claims,
               ////This token will be valid for 1 day
               expires: DateTime.Now.AddDays(1),
               signingCredentials: creds



                );
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}
