using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniERP.Entities;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class KullaniciController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIslemLogService _logService;
    private readonly string _sistemAdminEmail;

    public KullaniciController(UserManager<ApplicationUser> userManager, IOptions<SeedOptions> seedOptions, IIslemLogService logService)
    {
        _userManager = userManager;
        _logService = logService;
        _sistemAdminEmail = seedOptions.Value.AdminEmail;
    }

    public async Task<IActionResult> Index()
    {
        var kullanicilar = new List<KullaniciListItemViewModel>();
        var users = await _userManager.Users
            .OrderByDescending(u => u.Email == _sistemAdminEmail)
            .ThenBy(u => u.AdSoyad)
            .ToListAsync();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var sistemAdmini = SistemAdminiMi(user);
            kullanicilar.Add(new KullaniciListItemViewModel
            {
                Id = user.Id,
                AdSoyad = user.AdSoyad,
                Email = user.Email ?? string.Empty,
                Birim = user.Birim,
                Yetki = AppRoles.GetDisplayNameForRole(roles.FirstOrDefault() ?? string.Empty),
                SistemAdmini = sistemAdmini
            });
        }

        return View(kullanicilar);
    }

    public IActionResult Create()
    {
        FormListeleriniYukle();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KullaniciCreateViewModel model)
    {
        var role = AppRoles.GetRoleForBirim(model.Birim);
        if (role == null)
        {
            ModelState.AddModelError(nameof(model.Birim), "Select a valid authorization unit.");
        }

        if (!ModelState.IsValid)
        {
            FormListeleriniYukle();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            AdSoyad = model.AdSoyad,
            Birim = model.Birim
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role!);
            await _logService.AddAsync(LogModul.Kullanici, LogIslem.Olusturma, $"{user.Email} user was created.");
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        FormListeleriniYukle();
        return View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (SistemAdminiMi(user))
        {
            TempData["Error"] = "The system admin cannot be edited.";
            return RedirectToAction("Index");
        }

        var model = new KullaniciEditViewModel
        {
            Id = user.Id,
            AdSoyad = user.AdSoyad,
            Email = user.Email ?? string.Empty,
            Birim = user.Birim
        };

        FormListeleriniYukle();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(KullaniciEditViewModel model)
    {
        var role = AppRoles.GetRoleForBirim(model.Birim);
        if (role == null)
        {
            ModelState.AddModelError(nameof(model.Birim), "Select a valid authorization unit.");
        }

        if (!ModelState.IsValid)
        {
            FormListeleriniYukle();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        if (SistemAdminiMi(user))
        {
            TempData["Error"] = "The system admin cannot be edited.";
            return RedirectToAction("Index");
        }

        user.AdSoyad = model.AdSoyad;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.Birim = model.Birim;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            FormListeleriniYukle();
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.YeniSifre))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, resetToken, model.YeniSifre);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                FormListeleriniYukle();
                return View(model);
            }
        }

        var mevcutRoller = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, mevcutRoller);
        await _userManager.AddToRoleAsync(user, role!);
        await _logService.AddAsync(LogModul.Kullanici, LogIslem.Guncelleme, $"{user.Email} user was updated.");

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (SistemAdminiMi(user))
        {
            TempData["Error"] = "The system admin cannot be deleted.";
            return RedirectToAction("Index");
        }

        if (_userManager.GetUserId(User) == user.Id)
        {
            TempData["Error"] = "You cannot delete your own user account.";
            return RedirectToAction("Index");
        }

        var email = user.Email ?? user.UserName ?? user.Id;
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction("Index");
        }

        await _logService.AddAsync(LogModul.Kullanici, LogIslem.Silme, $"{email} user was deleted.");
        return RedirectToAction("Index");
    }

    private void FormListeleriniYukle()
    {
        ViewBag.YetkiBirimleri = AppRoles.YetkiBirimleri;
    }

    private bool SistemAdminiMi(ApplicationUser user)
    {
        return string.Equals(user.Email, _sistemAdminEmail, StringComparison.OrdinalIgnoreCase);
    }
}
