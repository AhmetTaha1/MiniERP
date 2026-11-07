using Microsoft.AspNetCore.Identity;

namespace MiniERP.Entities;

public class ApplicationUser : IdentityUser
{
    public string AdSoyad { get; set; } = string.Empty;
    public string Birim { get; set; } = string.Empty;
}
