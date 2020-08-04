using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.ManageUser)]
    [Area("Admin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }

        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostEnvironment;
            MenuItemVM = new MenuItemViewModel()
            {
                Category = _db.Category,
                menuItem = new MenuItem()
            };
        }
        public async Task<IActionResult> Index()
        {
            var menuItems = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync();
            return View(menuItems);
        }

        public IActionResult Create()
        {
            return View(MenuItemVM);
        }

        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            MenuItemVM.menuItem.SubCategoryID = Convert.ToInt32(Request.Form["SubCategoryId"]);
            if (!ModelState.IsValid)
            {
                return View(MenuItemVM);
            }
            _db.Add(MenuItemVM.menuItem);
            await _db.SaveChangesAsync();

            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;
            var menuItemFromDb = await _db.MenuItem.FindAsync(MenuItemVM.menuItem.Id);

            if (files.Count() > 0)
            {
                var uploads = Path.Combine(webRootPath, "Images");
                var extensions = Path.GetExtension(files[0].FileName);

                using (var fileStream = new FileStream(Path.Combine(uploads, MenuItemVM.menuItem.Id + extensions), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }

                menuItemFromDb.Image = @"\Images\" + MenuItemVM.menuItem.Id + extensions;
            }
            else
            {
                var uploads = Path.Combine(webRootPath, @"Images\" + SD.DefaultFoodImage);
                System.IO.File.Copy(uploads, webRootPath + @"\Images\" + MenuItemVM.menuItem.Id + ".jpg");
                menuItemFromDb.Image = @"\Images\" + MenuItemVM.menuItem.Id + ".jpg";
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Edit(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            MenuItemVM.menuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == Id);
            MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.menuItem.CategoryID).ToListAsync();
            if (MenuItemVM.menuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            MenuItemVM.menuItem.SubCategoryID = Convert.ToInt32(Request.Form["SubCategoryId"]);
            if (!ModelState.IsValid)
            {
                MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.menuItem.CategoryID).ToListAsync();
                return View(MenuItemVM);
            }

            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;
            var menuItemFromDb = await _db.MenuItem.FindAsync(MenuItemVM.menuItem.Id);

            if (files.Count() > 0)
            {
                var uploads = Path.Combine(webRootPath, "Images");
                var extensions = Path.GetExtension(files[0].FileName);

                if (menuItemFromDb.Image != null)
                {
                    var deleteImage = Path.Combine(webRootPath, menuItemFromDb.Image.Trim('\\'));
                    if (System.IO.File.Exists(deleteImage))
                    {
                        System.IO.File.Delete(deleteImage);
                    }
                }

                using (var fileStream = new FileStream(Path.Combine(uploads, MenuItemVM.menuItem.Id + extensions), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }

                menuItemFromDb.Image = @"\Images\" + MenuItemVM.menuItem.Id + extensions;
            }

            menuItemFromDb.Name = MenuItemVM.menuItem.Name;
            menuItemFromDb.Description = MenuItemVM.menuItem.Description;
            menuItemFromDb.Price = MenuItemVM.menuItem.Price;
            menuItemFromDb.Spicyness = MenuItemVM.menuItem.Spicyness;
            menuItemFromDb.CategoryID = MenuItemVM.menuItem.CategoryID;
            menuItemFromDb.SubCategoryID = MenuItemVM.menuItem.SubCategoryID;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Details(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            MenuItemVM.menuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == Id);
            MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.menuItem.CategoryID).ToListAsync();
            if (MenuItemVM.menuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReDirectToEdit(int? Id)
        {
            return RedirectToAction("Edit", new { Id });
        }

        public async Task<ActionResult> Delete(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            MenuItemVM.menuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == Id);
            MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.menuItem.CategoryID).ToListAsync();
            if (MenuItemVM.menuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            MenuItemVM.menuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == Id);
            MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.menuItem.CategoryID).ToListAsync();
            if (MenuItemVM.menuItem == null)
            {
                return NotFound();
            }
            _db.Remove(MenuItemVM.menuItem);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }
    }
}