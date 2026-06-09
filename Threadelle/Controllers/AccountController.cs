using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Threadelle.Models;

namespace Threadelle.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser>   _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser>   userManager)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
        }

        // GET /Account/login-partial
        [HttpGet("login-partial")]
        public IActionResult LoginPartial()
        {
            return PartialView("_LoginPartial");
        }

        // POST /Account/ajax-login
        [HttpPost("ajax-login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjaxLogin(
            [FromForm] string  email,
            [FromForm] string  password,
            [FromForm] bool    rememberMe,
            [FromForm] string? returnUrl)
        {
            var errs = new List<string>();
            if (string.IsNullOrWhiteSpace(email))    errs.Add("Email is required.");
            if (string.IsNullOrWhiteSpace(password)) errs.Add("Password is required.");
            if (errs.Any()) return Json(new { success = false, errors = errs });

            var result = await _signInManager.PasswordSignInAsync(
                email.Trim(), password, rememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var redirect = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
                return Json(new { success = true, redirect });
            }

            if (result.IsLockedOut)
                return Json(new { success = false, errors = new[] { "Account is locked. Please try again later." } });

            return Json(new { success = false, errors = new[] { "Invalid email or password. Please try again." } });
        }

        // POST /Account/ajax-register
        [HttpPost("ajax-register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjaxRegister(
            [FromForm] string  fullName,
            [FromForm] string  email,
            [FromForm] string  password,
            [FromForm] string  confirmPassword,
            [FromForm] int     gender,
            [FromForm] string? returnUrl)
        {
            var errs = new List<string>();
            if (string.IsNullOrWhiteSpace(fullName))   errs.Add("Full name is required.");
            if (string.IsNullOrWhiteSpace(email))      errs.Add("Email is required.");
            if (string.IsNullOrWhiteSpace(password))   errs.Add("Password is required.");
            if (password != confirmPassword)           errs.Add("Passwords do not match.");
            if (errs.Any()) return Json(new { success = false, errors = errs });

            var user = new ApplicationUser
            {
                UserName = email.Trim(),
                Email    = email.Trim(),
                FullName = fullName.Trim(),
                Gender   = (ApplicationUserGender)gender
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                var redirect = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
                return Json(new { success = true, redirect });
            }

            return Json(new { success = false, errors = result.Errors.Select(e => e.Description).ToList() });
        }
    }
}
