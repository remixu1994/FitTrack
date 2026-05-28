using FitTrack.Copilot.Service;
using Microsoft.AspNetCore.Identity;

namespace FitTrack.Copilot.Data;

public static class DataSeeder
{
    private const string AdminEmail = "admin@fittrack.local";
    private const string AdminPassword = "FitTrack123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var tenantBootstrapService = scope.ServiceProvider.GetRequiredService<ITenantBootstrapService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await tenantBootstrapService.EnsureSystemTenantAsync();
        await EnsureAdminRoleAsync(roleManager);
        await EnsureAdminUserAsync(userManager);
        await EnsureAdminRoleAssignmentAsync(userManager);
    }

    private static async Task EnsureAdminRoleAsync(RoleManager<IdentityRole> roleManager)
    {
        if (await roleManager.RoleExistsAsync(TenantConstants.AdminRole))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole(TenantConstants.AdminRole));
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin role: {errors}");
        }
    }

    private static async Task EnsureAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is not null)
        {
            if (string.IsNullOrWhiteSpace(admin.TenantId))
            {
                admin.TenantId = TenantConstants.DefaultTenantId;
                var updateResult = await userManager.UpdateAsync(admin);
                if (!updateResult.Succeeded)
                {
                    var updateErrors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign tenant to admin user: {updateErrors}");
                }
            }

            return;
        }

        admin = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true,
            TenantId = TenantConstants.DefaultTenantId
        };

        var result = await userManager.CreateAsync(admin, AdminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }
    }

    private static async Task EnsureAdminRoleAssignmentAsync(UserManager<ApplicationUser> userManager)
    {
        var admin = await userManager.FindByEmailAsync(AdminEmail)
            ?? throw new InvalidOperationException("The seeded admin user could not be loaded.");

        if (await userManager.IsInRoleAsync(admin, TenantConstants.AdminRole))
        {
            return;
        }

        var result = await userManager.AddToRoleAsync(admin, TenantConstants.AdminRole);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign admin role: {errors}");
        }
    }
}
