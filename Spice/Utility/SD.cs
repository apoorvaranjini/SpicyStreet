using Microsoft.AspNetCore.Server.IIS.Core;
using Spice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Utility
{
    public static class SD
    {
        public const string DefaultFoodImage = "default.jpg";
        public const string ManageUser = "Manager";
        public const string KitchenUser = "Kitchen";
        public const string FrontDeskUser = "FrontDesk";
        public const string CustomerEndUser = "Customer";
        public const string ShoppingCartCount = "ssCartCount";
		public const string CouponCode = "ssCouponCode";
		public const string StatusSubmitted = "Submitted";
		public const string StatusInProgress = "Being Prepared";
		public const string StatusReady = "Ready for Pickup";
		public const string StatusCompleted = "Completed";
		public const string StatusCancelled = "Cancelled";

		public const string PaymentStatusPending = "Pending";
		public const string PaymentStatusApproved = "Approved";
		public const string PaymentStatusRejected = "Rejected";
		public static string ConvertToRawHtml(string source)
		{
			if (source != null)
			{
				char[] array = new char[source.Length];
				int arrayIndex = 0;
				bool inside = false;

				for (int i = 0; i < source.Length; i++)
				{
					char let = source[i];
					if (let == '<')
					{
						inside = true;
						continue;
					}
					if (let == '>')
					{
						inside = false;
						continue;
					}
					if (!inside)
					{
						array[arrayIndex] = let;
						arrayIndex++;
					}
				}
				return new string(array, 0, arrayIndex);
			}
			else
			{
				return new string(string.Empty);
			}
		}

		public static double DiscountPrice(Coupon couponFromDB,double originalOrderTotal)
		{
			if(couponFromDB==null)
			{
				return originalOrderTotal;
			}
			
		    else if (couponFromDB.MinimumAmount > originalOrderTotal)
			{
					return originalOrderTotal;
			}
			else
			{
				if(Convert.ToInt32(couponFromDB.CouponType)==(int)Coupon.ECouponType.Dollar)
				{
					return Math.Round(originalOrderTotal - couponFromDB.Discount, 2);
				}
				if(Convert.ToInt32(couponFromDB.CouponType) == (int)Coupon.ECouponType.Percentge)
				{
					return Math.Round(originalOrderTotal -(originalOrderTotal * couponFromDB.Discount/100), 2);
				}
			}

			return originalOrderTotal;
			
		}
	}
}
