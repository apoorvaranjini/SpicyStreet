using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models
{
    public class SubCategory
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Sub Category Name ")]
        [Required]
        public string Name { get; set; }

        [Display(Name ="Category")]
        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category category { get; set; }
    }
}
