using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiLifeOS.API.Data;
using AiLifeOS.API.Models;
using MailKit.Net.Smtp;
using MimeKit;

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
            return Ok(new { message = "Reset token saved but email failed: " + ex.Message });
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

        var message = new MimeMessage();
        var emailUsername = _config["Email:Username"] ?? throw new InvalidOperationException("Email username not configured");
        message.From.Add(new MailboxAddress("AI Life OS", emailUsername));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = "Password Reset - AI Life OS";
        message.Body = new TextPart("html")
        {
            Text = $@"
                <h2>Password Reset</h2>
                <p>Click the link below to reset your password:</p>
                <a href='{resetLink}'>Reset Password</a>
                <p>This link expires in 1 hour.</p>
            "
        };

        using var client = new SmtpClient();
        client.Timeout = 10000;

        var emailHost = _config["Email:Host"] ?? throw new InvalidOperationException("Email host not configured");
        var emailPort = int.Parse(_config["Email:Port"] ?? "587");
        var emailUser = _config["Email:Username"] ?? throw new InvalidOperationException("Email username not configured");
        var emailPass = _config["Email:Password"] ?? throw new InvalidOperationException("Email password not configured");

        await client.ConnectAsync(
            emailHost,
            emailPort,
            MailKit.Security.SecureSocketOptions.StartTls
        );
        await client.AuthenticateAsync(
            emailUser,
            emailPass
        );
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}