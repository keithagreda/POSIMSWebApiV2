using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Dtos.ProductCategory
{
    public class ProductCategoryDto
    {
        public int? Id { get; set; }
        [Required]
        public string Name { get; set; }
    }

    public class ProductCategoryDtoV1
    {
        public int? Id { get; set; }
        public string Name { get; set; }
    }
}
