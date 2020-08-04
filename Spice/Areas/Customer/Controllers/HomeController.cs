using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            IndexViewModel IndexVM = new IndexViewModel()
            {
                MenuItem = await  _db.MenuItem.Include(m=>m.Category).Include(m=>m.SubCategory).ToListAsync(),
                Category =await  _db.Category.ToListAsync(),
                Coupon = await _db.Coupon.Where(c=>c.IsActive==true).ToListAsync()
            };

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if(claim !=null)
            {
                var cnt = _db.ShoppingCart.Where(u => u.ApplicationUserId == claim.Value).ToList().Count();
                HttpContext.Session.SetInt32(SD.ShoppingCartCount, cnt);
            }
            return View(IndexVM);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var menuItemFromDB = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).Where(m => m.Id == id).FirstOrDefaultAsync();
            ShoppingCart cartObj = new ShoppingCart()
            {
                MenuItem = menuItemFromDB,
                MenuItemId = menuItemFromDB.Id
            };
            return View(cartObj);
        }

        [HttpPost,ActionName("Details")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddtoCart(ShoppingCart cartObj)
        {
            cartObj.Id = 0;
           
            if(ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                cartObj.ApplicationUserId = claims.Value;
                ShoppingCart cartFromDB = await _db.ShoppingCart
                    .Where(c => c.ApplicationUserId == cartObj.ApplicationUserId && c.MenuItemId == cartObj.MenuItemId).
                    FirstOrDefaultAsync();
                if(cartFromDB ==null)
                {
                   await _db.ShoppingCart.AddAsync(cartObj);
                   
                }
                else
                {
                    cartFromDB.Count = cartFromDB.Count + cartObj.Count;
                }
                await _db.SaveChangesAsync();
                var count = _db.ShoppingCart.Where(c => c.ApplicationUserId == cartObj.ApplicationUserId).ToList().Count();
                HttpContext.Session.SetInt32(SD.ShoppingCartCount, count);
                return RedirectToAction("Index");
            }
            else
            {
                var menuItemFromDB = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).Where(m => m.Id == cartObj.MenuItemId).FirstOrDefaultAsync();
                ShoppingCart cart = new ShoppingCart()
                {
                    MenuItem = menuItemFromDB,
                    MenuItemId = menuItemFromDB.Id
                };
                return View(cart);
            }
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
