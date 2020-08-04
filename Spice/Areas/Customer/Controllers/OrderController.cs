using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private int PageSize = 2;
        public OrderController(ApplicationDbContext db,IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        //[Authorize]
        public async Task<IActionResult> Confirm(int Id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == Id && o.UserID == claim.Value),
                OrderDetails = await _db.OrderDetail.Where(o => o.OrderId == Id).ToListAsync()
            };
            return View(orderDetailsViewModel);
        }
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage=1)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            List<OrderHeader> orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(u => u.UserID == claim.Value).ToListAsync();

            foreach (OrderHeader item in orderHeaderList)
            {
                OrderDetailsViewModel indivisual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetail.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderListVM.Orders.Add(indivisual);
            }

            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id).Skip((productPage - 1) * PageSize).Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemPerPage = PageSize,
                TotalItem = count,
                urlParam = "/Customer/Order/OrderHistory?productPage=:"
            };
            return View(orderListVM);
        }

        [Authorize(Roles =SD.ManageUser+","+SD.KitchenUser)]
        public async Task<IActionResult> ManageOrder()
        {

            List<OrderDetailsViewModel> orderDetailsVM = new List<OrderDetailsViewModel>();
           
            List<OrderHeader> orderHeaderList = await _db.OrderHeader.Where(u => u.Status == SD.StatusSubmitted||u.Status==SD.StatusInProgress).OrderByDescending(o=>o.PickupTime).ToListAsync();

            foreach (OrderHeader item in orderHeaderList)
            {
                OrderDetailsViewModel indivisual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetail.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderDetailsVM.Add(indivisual);
            }
            // return View(orderDetailsVM.OrderByDescending(o=>o.OrderHeader.PickupTime));
            return View(orderDetailsVM);
        }

        public async Task<IActionResult> GetOrderDetails(int Id)
        {
            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.FirstOrDefaultAsync(m => m.Id == Id),
                OrderDetails = await _db.OrderDetail.Where(m => m.OrderId == Id).ToListAsync()
            };

            orderDetailsViewModel.OrderHeader.ApplicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == orderDetailsViewModel.OrderHeader.UserID);
            return PartialView("_IndivisualOrderDetails", orderDetailsViewModel);
            //return View(orderDetailsViewModel);

            //return PartialView("_Test");
        }

        public async Task<IActionResult> GetOrderStatus(int Id)
        {
            return PartialView("_Test");
        }

        [Authorize(Roles =SD.KitchenUser +","+SD.ManageUser)]
        public async Task<IActionResult> OrderPrepare(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusInProgress;
            await _db.SaveChangesAsync();
            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManageUser)]
        public async Task<IActionResult> OrderReady(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusReady;
            await _db.SaveChangesAsync();
            await _emailSender.SendEmailAsync(_db.Users.Where(u => u.Id == orderHeader.UserID).FirstOrDefault().Email, "Spice -Order Ready for PickUp" + orderHeader.Id.ToString(), "Order is ready for pickup");
            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManageUser)]
        public async Task<IActionResult> OrderCancel(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(OrderId);
            orderHeader.Status = SD.StatusCancelled;
            await _db.SaveChangesAsync();
            await _emailSender.SendEmailAsync(_db.Users.Where(u => u.Id == orderHeader.UserID).FirstOrDefault().Email, "Spice -Order Cancelled" + orderHeader.Id.ToString(), "Order as been cancelled  successfully");
            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize]
        public async Task<IActionResult> OrderPickup(int productPage = 1,string searchName=null,string searchPhone=null,string searchEmail=null)
        {
            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            StringBuilder param = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?productPage=:");
            param.Append("&searchName=");
            if(searchName!=null)
            {
                param.Append(searchName);
            }
            param.Append("&searchPhone=");
            if (searchPhone != null)
            {
                param.Append(searchPhone);
            }
            param.Append("&searchEmail=");
            if (searchEmail != null)
            {
                param.Append(searchEmail);
            }
            List<OrderHeader> orderHeaderList = new List<OrderHeader>();
            if (searchName != null || searchPhone != null || searchEmail != null)
            {
                var User = new ApplicationUser();
                if (searchName != null)
                {
                    orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                    .Where(u => u.PickupName.ToLower().Contains(searchName.ToLower())).
                    OrderByDescending(o => o.OrderDate).ToListAsync();
                }
                else
                {

                    if (searchEmail != null)
                    {
                        User = await _db.ApplicationUser.Where(u => u.Email.ToLower().Contains(searchEmail.ToLower())).FirstOrDefaultAsync();
                        orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                       .Where(u => u.ApplicationUser.Email.ToLower().Contains(searchEmail.ToLower())).OrderByDescending(o => o.OrderDate).ToListAsync();
                    }
                    else
                    {
                        if (searchPhone != null)
                        {
                            orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                            .Where(u => u.PhoneNumber.Contains(searchPhone)).
                            OrderByDescending(o => o.OrderDate).ToListAsync();
                        }
                    }
                }

            }
            else
            {
                orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(u => u.Status == SD.StatusReady).ToListAsync();
            }
                foreach (OrderHeader item in orderHeaderList)
                {
                    OrderDetailsViewModel indivisual = new OrderDetailsViewModel
                    {
                        OrderHeader = item,
                        OrderDetails = await _db.OrderDetail.Where(o => o.OrderId == item.Id).ToListAsync()
                    };
                    orderListVM.Orders.Add(indivisual);
                }
            
            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id).Skip((productPage - 1) * PageSize).Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemPerPage = PageSize,
                TotalItem = count,
                urlParam = param.ToString()
            };
            return View(orderListVM);
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManageUser)]
        [HttpPost]
        [ActionName("OrderPickup")]
        public async Task<IActionResult> OrderPickupPost(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(orderId);
            orderHeader.Status = SD.StatusCompleted;
            await _db.SaveChangesAsync();
            await _emailSender.SendEmailAsync(_db.Users.Where(u => u.Id == orderHeader.UserID).FirstOrDefault().Email, "Spice -Order completed" + orderHeader.Id.ToString(), "Order as been completed  successfully");
            return RedirectToAction("OrderPickup", "Order");
        }
    }
}