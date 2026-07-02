using System.Security.Claims;
using AccessTrust.Web.Services.Auth;
using AccessTrust.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(model.Correo, model.Password);

        if (!result.Success || result.Usuario is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        var usuario = result.Usuario;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id ?? string.Empty),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Correo)
        };

        foreach (var rol in usuario.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal
        );

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectByRole(usuario.Roles);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectByRole(List<string> roles)
    {
        if (roles.Contains("Administrador"))
        {
            return RedirectToAction("Administrador", "Dashboard");
        }

        if (roles.Contains("Aprobador"))
        {
            return RedirectToAction("Aprobador", "Dashboard");
        }

        if (roles.Contains("Solicitante"))
        {
            return RedirectToAction("Solicitante", "Dashboard");
        }

        return RedirectToAction("Index", "Home");
    }
}