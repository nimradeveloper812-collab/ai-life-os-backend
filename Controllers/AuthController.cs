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

        var emailHtml = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0;padding:0;background:#f8fafc;font-family:Arial,sans-serif'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f8fafc;padding:40px 0'>
    <tr>
      <td align='center'>
        <table width='520' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08)'>
          <tr>
            <td style='background:#2563eb;padding:36px 40px;text-align:center'>
              <h1 style='margin:0;color:#ffffff;font-size:26px;font-weight:700'>⚡ AI Life OS</h1>
              <p style='margin:8px 0 0;color:#bfdbfe;font-size:14px'>Your Personal AI Life Operating System</p>
            </td>
          </tr>
          <tr>
            <td style='padding:40px'>
              <h2 style='margin:0 0 16px;color:#1e293b;font-size:22px;font-weight:600'>Reset Your Password 🔑</h2>
              <p style='margin:0 0 24px;color:#64748b;font-size:15px;line-height:1.6'>
                We received a request to reset your password. Click the button below to create a new password. This link will expire in <strong>1 hour</strong>.
              </p>
              <table width='100%' cellpadding='0' cellspacing='0'>
                <tr>
                  <td align='center' style='padding:8px 0 32px'>
                    <a href='{resetLink}' style='background:#2563eb;color:#ffffff;padding:14px 36px;border-radius:10px;text-decoration:none;font-size:15px;font-weight:600;display:inline-block'>
                      Reset My Password →
                    </a>
                  </td>
                </tr>
              </table>
              <div style='background:#f1f5f9;border-radius:8px;padding:16px;margin-bottom:24px'>
                <p style='margin:0 0 6px;color:#94a3b8;font-size:12px'>Or copy this link:</p>
                <p style='margin:0;color:#2563eb;font-size:13px;word-break:break-all'>{resetLink}</p>
              </div>
              <div style='border-left:4px solid #f59e0b;padding:12px 16px;background:#fffbeb;border-radius:0 8px 8px 0'>
                <p style='margin:0;color:#92400e;font-size:13px'>
                  If you did not request this, please ignore this email.
                </p>
              </div>
            </td>
          </tr>
          <tr>
            <td style='background:#f8fafc;padding:24px 40px;border-top:1px solid #e2e8f0;text-align:center'>
              <p style='margin:0;color:#94a3b8;font-size:13px'>© 2024 AI Life OS · All rights reserved</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

        var body = new
        {
            from = "AI Life OS <noreply@afaqmart.store>",
            to = new[] { email },
            subject = "Password Reset - AI Life OS",
            html = emailHtml
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
