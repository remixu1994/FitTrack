using FitTrack.Copilot.Service;
using Microsoft.AspNetCore.Identity;

namespace FitTrack.Copilot.Data;

public static class DataSeeder
{
    private const string DefaultAdminEmail = "admin@fittrack.local";
    private const string DefaultAdminPassword = "FitTrack123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var tenantBootstrapService = scope.ServiceProvider.GetRequiredService<ITenantBootstrapService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var adminOptions = AdminSeedOptions.FromConfiguration(configuration);

        await tenantBootstrapService.EnsureSystemTenantAsync();
        await EnsureAdminRoleAsync(roleManager);
        await EnsureAdminUserAsync(userManager, adminOptions);
        await EnsureAdminRoleAssignmentAsync(userManager, adminOptions.Email);
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

    private static async Task EnsureAdminUserAsync(UserManager<ApplicationUser> userManager, AdminSeedOptions options)
    {
        var admin = await userManager.FindByEmailAsync(options.Email);
        if (admin is not null)
        {
            var shouldUpdate = false;

            if (string.IsNullOrWhiteSpace(admin.TenantId))
            {
                admin.TenantId = TenantConstants.DefaultTenantId;
                shouldUpdate = true;
            }

            if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true;
                shouldUpdate = true;
            }

            if (!string.Equals(admin.UserName, options.Email, StringComparison.OrdinalIgnoreCase))
            {
                admin.UserName = options.Email;
                shouldUpdate = true;
            }

            if (shouldUpdate)
            {
                var updateResult = await userManager.UpdateAsync(admin);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to update admin user: {errors}");
                }
            }

            if (options.ResetPasswordOnStartup)
            {
                await ResetPasswordAsync(userManager, admin, options.Password);
            }

            return;
        }

        admin = new ApplicationUser
        {
            UserName = options.Email,
            Email = options.Email,
            EmailConfirmed = true,
            TenantId = TenantConstants.DefaultTenantId
        };

        var result = await userManager.CreateAsync(admin, options.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }
    }

    private static async Task EnsureAdminRoleAssignmentAsync(UserManager<ApplicationUser> userManager, string adminEmail)
    {
        var admin = await userManager.FindByEmailAsync(adminEmail)
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

    private static async Task ResetPasswordAsync(UserManager<ApplicationUser> userManager, ApplicationUser admin, string password)
    {
        var hasPassword = await userManager.HasPasswordAsync(admin);
        IdentityResult result;

        if (hasPassword)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(admin);
            result = await userManager.ResetPasswordAsync(admin, token, password);
        }
        else
        {
            result = await userManager.AddPasswordAsync(admin, password);
        }

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to reset admin password: {errors}");
        }
    }

    private sealed record AdminSeedOptions(string Email, string Password, bool ResetPasswordOnStartup)
    {
        public static AdminSeedOptions FromConfiguration(IConfiguration configuration)
        {
            var email = configuration["Admin:Email"];
            var password = configuration["Admin:Password"];
            var resetPasswordOnStartup = configuration.GetValue("Admin:ResetPasswordOnStartup", false);

            if (string.IsNullOrWhiteSpace(email))
            {
                email = DefaultAdminEmail;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                password = DefaultAdminPassword;
            }

            return new AdminSeedOptions(email.Trim(), password, resetPasswordOnStartup);
        }
    }
}
