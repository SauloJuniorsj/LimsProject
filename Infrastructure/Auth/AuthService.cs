using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LimsProject.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LimsProject.Infrastructure.Auth;

public class AuthService(UserManager<IdentityUser> userManager, IConfiguration config) : IAuthService
{
    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
            return null;

        var roles = await userManager.GetRolesAsync(user);
        return GenerateToken(user, roles);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string role)
    {
        var user = new IdentityUser { Email = email, UserName = email };
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
            return new AuthResult(false, result.Errors.Select(e => e.Description));

        await userManager.AddToRoleAsync(user, role);
        return new AuthResult(true, []);
    }

    private string GenerateToken(IdentityUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
