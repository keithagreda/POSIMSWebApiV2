using POSIMSWebApi.Application.Dtos.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Dtos.Inventory
{
    public class CurrentInventoryDto
    {
        public string ProductName { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal SalesQty { get; set; }
        public decimal BegQty { get; set; }
        public decimal CurrentStocks { get; set; }
    }

    public class GetInventoryDto
    {
        public Guid InventoryId { get; set; }
        public string ProductName { get; set; }
        public decimal BegQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal SalesQty { get; set; }
        public DateTimeOffset? InventoryBegTime { get; set; }
        public DateTimeOffset? InventoryEndTime { get; set;}
    }

    public class CurrentInventoryV1Dto
    {
        public int ProductId { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal SalesQty { get; set; }
        public decimal BegQty { get; set; }
        public decimal CurrentStocks { get; set; }
    }

    public class CreateBeginningEntryDto
    {
        public int ProductId { get; set; }
        //public string ProductName { get; set;}
        public decimal ReceivedQty { get; set;}
    }

    public class ProductQuantityDto
    {
        public string? ProductName { get; set; }
        public int ProductId { get; set; }
        public decimal TotalQuantity { get; set; }
    }

    public class GetAllInventoryDto
    {
        public Guid InventoryBeginningId { get; set; }
        public List<ProductInventoryDto> ProductInventoryDtos { get; set; }
    }

    public class ProductInventoryDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal TotalQuantity { get; set; }
    }

    public class InventoryFilter : PaginationParams
    {
        public DateTime? MinCreationTime { get; set; }
        public DateTime? MaxCreationTime { get; set; }
        public DateTime? MinClosedTime { get; set; }
        public DateTime? MaxClosedTime { get; set; }
    }
}
