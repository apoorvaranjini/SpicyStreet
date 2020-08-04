using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models
{
    public class MenuItem
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public String Name { get; set; }
        public string  Description { get; set; }
        public string Image { get; set; }
        public string  Spicyness { get; set; }
        public enum ESpicy { NA=0,NotSpicy=1,Spicy=2,VerySpicy=3}

        [Range(1,int.MaxValue,ErrorMessage ="Price should be greater than ${1} ")]
        public double Price { get; set; }

        [Display(Name ="Category")]
        public int CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; }


        [Display(Name = "SubCategory")]
        public int SubCategoryID { get; set; }

        [ForeignKey("SubCategoryID")]
        public virtual SubCategory SubCategory { get; set; }
    }
}
