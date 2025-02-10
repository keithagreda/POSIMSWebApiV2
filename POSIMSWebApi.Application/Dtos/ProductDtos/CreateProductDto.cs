using Domain.Entities;
using POSIMSWebApi.Application.Dtos.ProductCategory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Dtos.ProductDtos
{
    public class CreateProductDto
    {
        public string Name { get; set; }
        public int DaysTillExpiration { get; set; }
        public decimal  Price { get; set; }
        public List<ProductCategoryDtoV1>? ProductCategories { get; set; } = new List<ProductCategoryDtoV1>();
    }

    public class CreateProductV1Dto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public int DaysTillExpiration { get; set; }
        public decimal Price { get; set; }
        public ProductCategoryDtoV1? ProductCategories { get; set; }
    }

    public class CreateProductSales
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public string Name { get; set; }
        public decimal ActualSellingPrice { get; set; }
        public decimal Quantity { get; set; }
    }
}
