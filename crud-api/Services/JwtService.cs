using crud_api.models;
using crud_api.Data;
using crud_api.common;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Diagnostics;
namespace crud_api.Services
{
    public class JwtService(FileDbContext dbContext, IConfiguration configuration)
    {
        private readonly FileDbContext _dbcontext = dbContext;
        private readonly IConfiguration _configuration = configuration;
        public LoginResponse? Authenticate(LoginRequest request){
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return null;
            var user = _dbcontext.Users.FirstOrDefault(x => x.Email == request.Email);
            if(user is null || Utilities.ComputeSHA256(request.Password) != user.Password || !IsTokenExpired(user.Token!)) return null;
            var Issuer = _configuration["JwtConfig:Issuer"];
            var Audience = _configuration["JwtConfig:Audience"];
            var Key =  _configuration["JwtConfig:Key"];
            var tokenValidityMins = _configuration.GetValue<int>("JwtConfig:TokenValidityMins");
            var tokenDescriptor = new SecurityTokenDescriptor 
            {
                Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString())]),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(tokenValidityMins),
                Issuer = Issuer,
                Audience = Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Key!)),SecurityAlgorithms.HmacSha256Signature),
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(securityToken);
            user.Token = accessToken;
            _dbcontext.SaveChanges();
            return new LoginResponse {
                AccessToken = accessToken,
                UserId = user.Id,
                ExpiresIn = tokenValidityMins
            };
    }
    public static bool IsTokenExpired(string token)
    {
        if(token == "" || token == null){
            return true;
        }
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var expiration = jwtToken.ValidTo;
        return expiration < DateTime.UtcNow;
    }
}
}