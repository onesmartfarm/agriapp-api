using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgriApp.Application.DTOs;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AgriApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserRepository _userRepo;
    private readonly IConfiguration _config;

    public AuthController(UserRepository userRepo, IConfiguration config)
    {
        _userRepo = userRepo;
        _config = config;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials" });

        var token = GenerateToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            User = MapUser(user)
        });
    }

    [HttpPost("register")]
    [Authorize(Roles = "SuperUser,Manager")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing != null)
            return Conflict(new { error = "Email already registered" });

        if (!Enum.TryParse<Role>(request.Role ?? "Staff", out var role))
            return BadRequest(new { error = "Invalid role" });

        var callerRole = Enum.Parse<Role>(User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value);
        if (role == Role.SuperUser && callerRole != Role.SuperUser)
            return Forbid();

        if (role == Role.Manager && callerRole != Role.SuperUser)
            return Forbid();

        var callerCenterClaim = User.FindFirst("CenterId")?.Value;
        int? centerId = request.CenterId;
        if (callerRole != Role.SuperUser)
        {
            if (callerCenterClaim != null)
                centerId = int.Parse(callerCenterClaim);
            else
                return BadRequest(new { error = "Manager must belong to a center to register users" });
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            CenterId = centerId,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.CreateAsync(user);
        var token = GenerateToken(user);

        return Created($"/api/users/{user.Id}", new AuthResponse
        {
            Token = token,
            User = MapUser(user)
        });
    }

    private string GenerateToken(User user)
    {
        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key not configured");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        if (user.CenterId.HasValue)
            claims.Add(new Claim("CenterId", user.CenterId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapUser(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role.ToString(),
        CenterId = user.CenterId
    };
}
