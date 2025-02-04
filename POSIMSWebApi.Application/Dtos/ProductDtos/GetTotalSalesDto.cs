namespace POSIMSWebApi.Application.Dtos.ProductDtos
{
    public class GetTotalSalesDto
    {
        public int TotalSales { get; set; }
        public int SalesPercentage { get; set; }
        public int[] AllSalesPercentage { get; set; } = [];
    }

    public class PerMonthSalesDto
    {
        public string Month { get; set; }
        public string Year { get; set; }
        public decimal SalesPercentage { get; set; }
        public decimal TotalMonthlySales { get; set; }
    }

    public class ViewSalesHeaderDto
    {
        public string TransNum { get; set; }
        public string CustomerName { get; set; }
        public Guid SoldById { get; set; }
        public string SoldBy { get; set; }
        public DateTimeOffset TransDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal FinalTotalAmount { get; set; }
        public decimal Discount { get; set; }
        public List<ViewSalesDetailDto> ViewSalesDetailDtos { get; set; } = new List<ViewSalesDetailDto>();

    }

    public class ViewSalesDetailDto
    {
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }
}
