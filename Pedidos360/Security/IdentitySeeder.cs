using Microsoft.AspNetCore.Identity;

namespace Pedidos360.Security;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(result, $"No se pudo crear el rol {roleName}");
            }
        }

        // Development credentials are configured only in appsettings.Development.json.
        // Other environments should supply InitialAdmin through secret configuration.
        var email = configuration["InitialAdmin:Email"];
        var password = configuration["InitialAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(admin, password);
            EnsureSucceeded(createResult, "No se pudo crear el usuario administrador inicial");
        }

        if (!await userManager.IsInRoleAsync(admin, RoleNames.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
            EnsureSucceeded(roleResult, "No se pudo asignar el rol Admin inicial");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"{message}: {errors}");
        }
    }
}
