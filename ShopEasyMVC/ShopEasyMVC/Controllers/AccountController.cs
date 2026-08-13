using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;
using ShopEasyMVC.Models;
using ShopEasyMVC.Services;

namespace ShopEasyMVC.Controllers
    {
    public class AccountController : Controller
        {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public AccountController(AppDbContext context, IEmailSender emailSender)
            {
            _context = context;
            _emailSender = emailSender;
            }

        [HttpGet]
        public IActionResult Register()
            {
            return View();
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
            {
            fullName = (fullName ?? string.Empty).Trim();
            email = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(fullName))
                {
                ModelState.AddModelError("fullName", "El nombre completo es obligatorio.");
                }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                {
                ModelState.AddModelError("password", "La contraseña debe tener al menos 6 caracteres.");
                }

            if (password != confirmPassword)
                {
                ModelState.AddModelError("confirmPassword", "Las contraseñas no coinciden.");
                }

            if (await _context.Users.AnyAsync(u => u.Email == email))
                {
                ModelState.AddModelError("email", "Ya existe una cuenta con este correo.");
                }

            if (!ModelState.IsValid)
                {
                return View();
                }

            var user = new User { FullName = fullName, Email = email };
            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, password);
            user.UserRoles.Add(new UserRole { Name = "cliente" });

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _emailSender.SendAsync(user.Email, "Bienvenido a ShopEasy",
                $"Hola {user.FullName}, tu cuenta fue creada exitosamente.");

            await SignInAsync(user);

            return RedirectToAction("Index", "Home");
            }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
            {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
            {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            ViewData["ReturnUrl"] = returnUrl;

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == email);

            var hasher = new PasswordHasher<User>();
            var verifyResult = user is null
                ? PasswordVerificationResult.Failed
                : hasher.VerifyHashedPassword(user, user.PasswordHash, password ?? string.Empty);

            if (user is null || verifyResult == PasswordVerificationResult.Failed)
                {
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                return View();
                }

            await SignInAsync(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                return Redirect(returnUrl);
                }

            return RedirectToAction("Index", "Home");
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
            {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
            }

        public IActionResult AccessDenied()
            {
            return View();
            }

        private async Task SignInAsync(User user)
            {
            var claims = new List<Claim>
                {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email)
                };

            foreach (var role in user.UserRoles)
                {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
                }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            }
        }
    }
