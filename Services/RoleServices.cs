﻿using EventVault.Models;
using EventVault.Models.DTOs.Identity;
using EventVault.Services.IServices;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EventVault.Services
{
    public class RoleServices : IRoleServices
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;

        public RoleServices(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public async Task InitalizeRolesAsync()
        {
            string[] roles = {"Admin", "User"};

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public async Task AssignRoleBasedOnUsernameAsync(User user)
        {
            if (!await _roleManager.RoleExistsAsync("Admin") || !await _roleManager.RoleExistsAsync("User"))
            {
                await InitalizeRolesAsync(); 
            }

            if (user.UserName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "User");
            }
        }

        public async Task AssignRoleGoogleAsync(User user, string roleName)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(roleName))
            {
                return; 
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                throw new System.Exception($"Failed to assign role '{roleName}' to user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
