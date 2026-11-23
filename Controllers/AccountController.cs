using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudentSupport.Data;
using StudentSupport.Models;
using StudentSupport.Services;

namespace StudentSupport.Controllers;
public class AccountController(AppDbContext db, IPasswordHasher<AppUser> hasher, SecurityService sec) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Demo") : View(new LoginInput());

    [AllowAnonymous, EnableRateLimiting("login"), HttpPost]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var user = await db.Users.SingleOrDefaultAsync(x => x.UserName == input.Username.Trim());
        const string invalid = "Tài khoản hoặc mật khẩu không hợp lệ.";
        if (user is null)
        {
            await sec.Log(null, "LOGIN_FAILED", null, false, "Unknown user");
            ModelState.AddModelError("", invalid);
            return View(input);
        }
        if (!user.IsActive || user.LockoutUntilUtc > DateTime.UtcNow)
        {
            await sec.Log(user.Id, "LOGIN_BLOCKED", null, false);
            ModelState.AddModelError("", "Tài khoản tạm bị khóa hoặc chưa được kích hoạt.");
            return View(input);
        }
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= 5) { user.LockoutUntilUtc = DateTime.UtcNow.AddMinutes(5); user.FailedAttempts = 0; }
            await db.SaveChangesAsync();
            await sec.Log(user.Id, "LOGIN_FAILED", null, false);
            ModelState.AddModelError("", invalid);
            return View(input);
        }
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, input.Password);
        user.FailedAttempts = 0;
        user.LockoutUntilUtc = null;
        await db.SaveChangesAsync();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName), new Claim(ClaimTypes.Role, user.Role) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        await sec.Log(user.Id, "LOGIN_SUCCESS", null, true);
        return RedirectToAction("Index", "Demo");
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpGet]
    public IActionResult Password() => View(new PasswordInput());

    [Authorize, HttpPost]
    public async Task<IActionResult> Password(PasswordInput model)
    {
        if (!ModelState.IsValid) return View(model);
        var userId = SecurityService.Id(User);
        if (!userId.HasValue) return Forbid();
        var me = await db.Users.SingleOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (me is null) return Forbid();
        if (hasher.VerifyHashedPassword(me, me.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
            await sec.Log(me.Id, "PASSWORD_CHANGE_DENIED", null, false);
            return View(model);
        }
        var pwd = model.NewPassword;
        if (!pwd.Any(char.IsUpper) || !pwd.Any(char.IsLower) || !pwd.Any(char.IsDigit) || !pwd.Any(c => !char.IsLetterOrDigit(c)))
        {
            ModelState.AddModelError("", "Mật khẩu mới cần chữ hoa, thường, số và ký tự đặc biệt.");
            return View(model);
        }
        me.PasswordHash = hasher.HashPassword(me, model.NewPassword);
        await db.SaveChangesAsync();
        await sec.Log(me.Id, "PASSWORD_CHANGED", null, true);
        await HttpContext.SignOutAsync();
        TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Denied() { Response.StatusCode = 403; return View(); }

    [AllowAnonymous]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return Content("Có lỗi nội bộ. Vui lòng liên hệ người quản trị; không hiển thị thông tin kỹ thuật.");
    }
}
