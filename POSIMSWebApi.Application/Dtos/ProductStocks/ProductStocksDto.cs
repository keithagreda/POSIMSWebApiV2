using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Dtos.ProductStocks
{
    public class ProductStocksDto
    {
        public int ProductId { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal SalesQty { get; set; }
        public decimal BegQty { get; set; }
        public Guid InventoryId { get; set; }
    }
}
