using JwtAuthDotNet9.Data;
using JwtAuthDotNet9.Entities;
using JwtAuthDotNet9.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JwtAuthDotNet9.Services
{
    public class AuthService(UserDbContext context,IConfiguration configuration) : IAuthService
    {
        public async Task<TokenResponseDto?> LoginAsync(UserDto request)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            
            if (user is null)
            {
                return null;
            }
           
            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return null;
            }
            var response=new TokenResponseDto
            { AccessToken=CreateToken(user),
              RefreshToken= await GenerateAndSaveRefreshTokenAsync(user)
            };
        
            return response;
        }
        private string GeneraeRefreshToken()
        {
                       var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GeneraeRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            await context.SaveChangesAsync();
            return refreshToken;
        }
        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                ////Here this time we have name so we are using name here otherwise we will be using ID here
                new Claim(ClaimTypes.Name,user.Username),
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Role,user.Role.ToString())
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

        public async Task<User?> RegisterAsync(UserDto request)
        {
            if(await context.Users.AnyAsync(u=>u.Username==request.Username))
            {
                return null;
            }
            var user=new User();

            var hashedPassword = new PasswordHasher<User>().HashPassword(user, request.Password);
            user.Username = request.Username;
            user.PasswordHash = hashedPassword;
            context.Users.Add(user);
           await  context.SaveChangesAsync();
            return user;
        }
    }
}
