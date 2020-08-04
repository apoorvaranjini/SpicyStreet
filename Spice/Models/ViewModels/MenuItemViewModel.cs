using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models.ViewModels
{
    public class MenuItemViewModel
    {
        public MenuItem menuItem { get; set; }
        public IEnumerable<Category> Category { get; set;}

        public IEnumerable<SubCategory> SubCategory { get; set; }
    }
}
