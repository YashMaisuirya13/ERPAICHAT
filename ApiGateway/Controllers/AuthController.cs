using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Shared.Events.Models;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("token")]
    public IActionResult GenerateToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Role))
        {
            return BadRequest(ApiResponse.Failed("Username and Role are required."));
        }

        var validRoles = new[] { "Admin", "Procurement", "Manager" };
        if (!validRoles.Contains(request.Role))
        {
            return BadRequest(ApiResponse.Failed($"Invalid role. Allowed roles are: {string.Join(", ", validRoles)}"));
        }

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "SuperSecretKeyForErpAiChatJwtTokenSecurity2026!LongEnoughForHmacSha256";
        var issuer = jwtSettings["Issuer"] ?? "ERP.ApiGateway";
        var audience = jwtSettings["Audience"] ?? "ERP.Services";

        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.Role, request.Role)
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(ApiResponse<object>.Succeeded(new
        {
            Token = tokenString,
            Username = request.Username,
            Role = request.Role,
            ExpiresAt = tokenDescriptor.Expires
        }, "Token generated successfully"));
    }
}

public class TokenRequest
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
