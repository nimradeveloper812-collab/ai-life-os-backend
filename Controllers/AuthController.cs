using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiLifeOS.API.Data;
using AiLifeOS.API.Models;

namespace AiLifeOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email already exists" });

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Registration successful", userId = user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        var token = GenerateToken(user);
        return Ok(new { token, userId = user.Id, name = user.Name, email = user.Email });
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                GoogleId = dto.GoogleId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var token = GenerateToken(user);
        return Ok(new { token, userId = user.Id, name = user.Name, email = user.Email });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return Ok(new { message = "If email exists, reset link sent" });

        var token = Guid.NewGuid().ToString();
        user.ResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        try
        {
            await SendResetEmail(user.Email, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EMAIL ERROR: {ex.Message}");
            return Ok(new { message = "Email failed: " + ex.Message });
        }

        return Ok(new { message = "Reset link sent to your email" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.ResetToken == dto.Token &&
            u.ResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return BadRequest(new { message = "Invalid or expired token" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password reset successful" });
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task SendResetEmail(string email, string token)
    {
        var frontendUrl = "https://ai-life-os-frontend.vercel.app";
        var resetLink = $"{frontendUrl}/reset-password?token={token}";

        var apiKey = _config["Resend:ApiKey"];
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var body = new
        {
            from = "AI Life OS <noreply@afaqmart.store>",
            to = new[] { email },
            subject = "Password Reset - AI Life OS",
            html = $@"
                <div style='font-family:Arial,sans-serif;max-width:500px;margin:0 auto;padding:20px'>
                    <h2 style='color:#2563eb'>🔑 Password Reset</h2>
                    <p>Click the button below to reset your password:</p>
                    <a href='{resetLink}' 
                       style='background:#2563eb;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>
                       Reset Password
                    </a>
                    <p style='color:#666;font-size:14px'>This link expires in 1 hour.</p>
                    <p style='color:#666;font-size:14px'>If you didn't request this, ignore this email.</p>
                </div>
            "
        };

        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.resend.com/emails", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Resend error: {error}");
        }
    }
}
