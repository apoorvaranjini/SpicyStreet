using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spice.Models;
using Spice.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Data
{
    public class DbInitializer : IDbIntializer
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser>_userManager;
        private readonly RoleManager<IdentityRole>_roleManager;

        public DbInitializer(ApplicationDbContext db,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async void Intializer()
        {
            try
            {
                if(_db.Database.GetPendingMigrations().Count()>0)
                {
                    _db.Database.Migrate();
                }
            }
            catch(Exception ex)
            {

            }

            if (_db.Roles.Any(r => r.Name == SD.ManageUser)) return;
            _roleManager.CreateAsync(new IdentityRole(SD.ManageUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.CustomerEndUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.KitchenUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.FrontDeskUser)).GetAwaiter().GetResult();

            _userManager.CreateAsync(new ApplicationUser
            {
                UserName = "admin@gmail.com",
                Email = "admin@gmail.com",
                Name = "Apoorva Ranjini",
                EmailConfirmed = true,
                PhoneNumber = "123456789"
            }, "Admin123!").GetAwaiter().GetResult();

            IdentityUser user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com");
            await _userManager.AddToRoleAsync(user, SD.ManageUser);

        }
    }
}
