using Microsoft.AspNetCore.Identity;

namespace FitTrack.Copilot.Data;

public static class DataSeeder
{
    private const string AdminEmail = "admin@fittrack.local";
    private const string AdminPassword = "FitTrack123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureAdminUserAsync(userManager);
    }

    private static async Task EnsureAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is not null)
        {
            return;
        }

        admin = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, AdminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }
    }
}
