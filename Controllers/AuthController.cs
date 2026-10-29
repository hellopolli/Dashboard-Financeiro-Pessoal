using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Claims;
using DashboardFinanceiroPessoal.Data;
using DashboardFinanceiroPessoal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardFinanceiroPessoal.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext context) : ControllerBase
{
    private const int Iterations = 100_000;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await context.Usuarios.AnyAsync(user => user.Email.ToLower() == email))
            return Conflict(new { message = "Este e-mail já está cadastrado." });

        var user = new Usuario
        {
            Nome = request.Nome.Trim(),
            Email = email,
            SenhaHash = HashPassword(request.Senha)
        };

        context.Usuarios.Add(user);
        await context.SaveChangesAsync();
        return Ok(new { id = user.Id, nome = user.Nome, email = user.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await context.Usuarios.FirstOrDefaultAsync(item => item.Email.ToLower() == email);
        if (user is null || !VerifyPassword(request.Senha, user.SenhaHash))
            return Unauthorized(new { message = "E-mail ou senha inválidos." });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Nome)
            ], CookieAuthenticationDefaults.AuthenticationScheme)));
        return Ok(new { id = user.Id, nome = user.Nome, email = user.Email, planoSelecionado = user.PlanoSelecionado });
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await context.Usuarios.FindAsync(id);
        return user is null ? Unauthorized() : Ok(new { id = user.Id, nome = user.Nome, email = user.Email,
            moeda = user.Moeda, planoSelecionado = user.PlanoSelecionado });
    }

    [Authorize, HttpPut("profile")]
    public async Task<IActionResult> Profile(ProfileRequest request)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await context.Usuarios.FindAsync(id);
        if (user is null) return Unauthorized();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await context.Usuarios.AnyAsync(u => u.Id != id && u.Email.ToLower() == email))
            return Conflict(new { message = "Este e-mail já está cadastrado." });
        user.Nome = request.Nome.Trim();
        user.Email = email;
        user.Moeda = request.Moeda;
        await context.SaveChangesAsync();
        return Ok(new { id = user.Id, nome = user.Nome, email = user.Email, moeda = user.Moeda,
            planoSelecionado = user.PlanoSelecionado });
    }

    [Authorize, HttpPut("password")]
    public async Task<IActionResult> Password(ChangePasswordRequest request)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await context.Usuarios.FindAsync(id);
        if (user is null) return Unauthorized();
        if (!VerifyPassword(request.SenhaAtual, user.SenhaHash))
            return BadRequest(new { message = "Senha atual incorreta." });
        user.SenhaHash = HashPassword(request.NovaSenha);
        await context.SaveChangesAsync();
        return Ok(new { message = "Senha alterada." });
    }

    [Authorize, HttpPost("plan")]
    public async Task<IActionResult> ChoosePlan()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await context.Usuarios.FindAsync(id);
        if (user is null) return Unauthorized();
        user.PlanoSelecionado = true;
        await context.SaveChangesAsync();
        return Ok(new { planoSelecionado = true });
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword(ForgotPasswordRequest request)
    {
        // Keep the response generic to avoid exposing whether an account exists.
        return Ok(new { message = "Se o endereço estiver cadastrado, enviaremos instruções para redefinir sua senha." });
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class RegisterRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string Nome { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; init; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Senha { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    [Required] public string Senha { get; init; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
}

public sealed class ProfileRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string Nome { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; init; } = string.Empty;
    [Required, RegularExpression("BRL|USD|EUR")] public string Moeda { get; init; } = "BRL";
}

public sealed class ChangePasswordRequest
{
    [Required] public string SenhaAtual { get; init; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string NovaSenha { get; init; } = string.Empty;
}
