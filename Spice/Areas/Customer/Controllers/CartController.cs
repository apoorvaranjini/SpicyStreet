using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Spice.Data;
using Spice.Extensions;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using Stripe;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        private readonly IEmailSender _emailSender;

        [BindProperty]
        public OrderDetailsCart DetailsCart { get; set; }
        public CartController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }
        public async Task<IActionResult> Index()
        {
            DetailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            DetailsCart.OrderHeader.OrderTotal = 0;
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value);
            if(cart !=null)
            {
                DetailsCart.listCart = cart.ToList();
            }

            foreach(var list in DetailsCart.listCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                DetailsCart.OrderHeader.OrderTotal = DetailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count);
                list.MenuItem.Description = SD.ConvertToRawHtml(list.MenuItem.Description);
                if(list.MenuItem.Description.Length >100)
                {
                    list.MenuItem.Description = list.MenuItem.Description.Substring(0, 99) + "...";
                }
            }
            DetailsCart.OrderHeader.OrderTotalOriginal = DetailsCart.OrderHeader.OrderTotal;
            if(HttpContext.Session.GetString(SD.CouponCode)!=null)
            {
                DetailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.CouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == DetailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                DetailsCart.OrderHeader.OrderTotal = SD.DiscountPrice(couponFromDb, DetailsCart.OrderHeader.OrderTotalOriginal);
            }
            return View(DetailsCart);
        }

        public async Task<IActionResult> Summary()
        {
            DetailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            DetailsCart.OrderHeader.OrderTotal = 0;
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ApplicationUser applicationUser = await _db.ApplicationUser.Where(c => c.Id == claim.Value).FirstOrDefaultAsync();
            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value);
            if (cart != null)
            {
                DetailsCart.listCart = cart.ToList();
            }

            foreach (var list in DetailsCart.listCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                DetailsCart.OrderHeader.OrderTotal = DetailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count);
            }
            DetailsCart.OrderHeader.OrderTotalOriginal = DetailsCart.OrderHeader.OrderTotal;
            DetailsCart.OrderHeader.PickupName = applicationUser.Name;
            DetailsCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            DetailsCart.OrderHeader.PickupTime = DateTime.Now;

            if (HttpContext.Session.GetString(SD.CouponCode) != null)
            {
                DetailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.CouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == DetailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                DetailsCart.OrderHeader.OrderTotal = SD.DiscountPrice(couponFromDb, DetailsCart.OrderHeader.OrderTotalOriginal);
            }
            return View(DetailsCart);
        }

        public IActionResult AddCoupon()
        {
            if(DetailsCart.OrderHeader.CouponCode==null)
            {
                DetailsCart.OrderHeader.CouponCode = "";
            }

            HttpContext.Session.SetString(SD.CouponCode,DetailsCart.OrderHeader.CouponCode);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(SD.CouponCode,string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> plus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> minus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);
            if (cart.Count == 1)
            {
                _db.ShoppingCart.Remove(cart);
                await _db.SaveChangesAsync();
                var cnt = _db.ShoppingCart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count();
                HttpContext.Session.SetInt32(SD.ShoppingCartCount, cnt);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> remove(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);
            
            _db.ShoppingCart.Remove(cart);
             await _db.SaveChangesAsync();

            var cnt = _db.ShoppingCart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count();
             HttpContext.Session.SetInt32(SD.ShoppingCartCount, cnt);
        
             return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(string stripeToken)
        {   
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            DetailsCart.listCart = await _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value).ToListAsync();
            DetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            DetailsCart.OrderHeader.OrderDate = DateTime.Now;
            DetailsCart.OrderHeader.UserID = claim.Value;
            DetailsCart.OrderHeader.Status = SD.PaymentStatusPending;
            DetailsCart.OrderHeader.PickupTime = Convert.ToDateTime(DetailsCart.OrderHeader.PickupDate.ToShortDateString() +" "+DetailsCart.OrderHeader.PickupTime.ToShortTimeString());

            List<OrderDetail> orderDetailsList = new List<OrderDetail>();
            _db.OrderHeader.Add(DetailsCart.OrderHeader);
            await _db.SaveChangesAsync();

            DetailsCart.OrderHeader.OrderTotalOriginal = 0;

            foreach (var item in DetailsCart.listCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                OrderDetail orderDetail = new OrderDetail()
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = DetailsCart.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                DetailsCart.OrderHeader.OrderTotalOriginal += orderDetail.Count * orderDetail.Price;
                _db.OrderDetail.Add(orderDetail);
            }

            if (HttpContext.Session.GetString(SD.CouponCode) != null)
            {
                DetailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.CouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == DetailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                DetailsCart.OrderHeader.OrderTotal = SD.DiscountPrice(couponFromDb, DetailsCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                DetailsCart.OrderHeader.OrderTotal = DetailsCart.OrderHeader.OrderTotalOriginal;
            }

            DetailsCart.OrderHeader.CouponCodeDiscount = DetailsCart.OrderHeader.OrderTotalOriginal - DetailsCart.OrderHeader.OrderTotal;
            await _db.SaveChangesAsync();

            _db.ShoppingCart.RemoveRange(DetailsCart.listCart);
            HttpContext.Session.SetInt32(SD.ShoppingCartCount, 0);
            await _db.SaveChangesAsync();

            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(DetailsCart.OrderHeader.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID:" + DetailsCart.OrderHeader.Id,
                SourceId = stripeToken

            };
            var service = new ChargeService();
            Charge charge = service.Create(options);
            if(charge.BalanceTransactionId==null)
            {
                DetailsCart.OrderHeader.Status = SD.PaymentStatusRejected;
            }
            else
            {
                DetailsCart.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }
            if(charge.Status.ToLower()=="succeeded")
            {
                await _emailSender.SendEmailAsync(_db.Users.Where(u => u.Id == claim.Value).FirstOrDefault().Email, "Spice -Order Created" + DetailsCart.OrderHeader.Id.ToString(), "Order as been submitted successfully");
                DetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                DetailsCart.OrderHeader.Status = SD.StatusSubmitted;

            }
            else
            {
               DetailsCart.OrderHeader.Status = SD.PaymentStatusRejected;
            }
            await _db.SaveChangesAsync();
            //return RedirectToAction("Index","Home");
            return RedirectToAction("Confirm","Order",new { id = DetailsCart.OrderHeader.Id });
        }
       
    }
}