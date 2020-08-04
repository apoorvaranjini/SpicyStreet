using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.ManageUser)]
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult>Index()
        {
            return View(await _db.Coupon.ToListAsync());
        }

        public IActionResult Create ()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count() > 0)
                {
                    byte[] P = null;
                    using (var fs = files[0].OpenReadStream())
                    {
                        using (var ms = new MemoryStream())
                        {
                            fs.CopyTo(ms);
                            P = ms.ToArray();
                        }
                    }
                    coupon.Picture = P;
                }
                _db.Coupon.Add(coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        public async Task<IActionResult> Edit(int? Id)
        {
            if (Id==null)
            {
                return NotFound();
            }

            var couponsfromDB = await _db.Coupon.FindAsync(Id);
            if (couponsfromDB == null)
            {
                return NotFound();
            }

            return View(couponsfromDB);
        }

        [HttpPost,ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count() > 0)
                {
                    byte[] P = null;
                    using (var fs = files[0].OpenReadStream())
                    {
                        using (var ms = new MemoryStream())
                        {
                            fs.CopyTo(ms);
                            P = ms.ToArray();
                        }
                    }
                    coupon.Picture = P;
                }
                var couponsfromDB = await _db.Coupon.FindAsync(coupon.Id);
                couponsfromDB.Name = coupon.Name;
                couponsfromDB.CouponType = coupon.CouponType;
                couponsfromDB.IsActive = coupon.IsActive;
                couponsfromDB.MinimumAmount = coupon.MinimumAmount;
                couponsfromDB.Discount = coupon.Discount;
                if(files.Count() > 0)
                {
                    couponsfromDB.Picture = coupon.Picture;
                }
                _db.Update(couponsfromDB);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        public async Task<IActionResult> Details(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }

            var couponsfromDB = await _db.Coupon.FindAsync(Id);
            if (couponsfromDB == null)
            {
                return NotFound();
            }

            return View(couponsfromDB);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReDirectToEdit(int? Id)
        {
            return RedirectToAction("Edit", new { Id });
        }

        public async Task<IActionResult> Delete(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }

            var couponsfromDB = await _db.Coupon.FindAsync(Id);
            if (couponsfromDB == null)
            {
                return NotFound();
            }

            return View(couponsfromDB);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            var couponsfromDB = await _db.Coupon.FindAsync(Id);
            if (couponsfromDB == null)
            {
                return NotFound();
            }
            _db.Remove(couponsfromDB);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }
    }
}