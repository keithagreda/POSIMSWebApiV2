namespace POSIMSWebApi.Application.Dtos.ProductDtos
{
    public class GetProductDropDownTableDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public bool showControl { get; set; } = false;
    }

    public class GetProductDropDownTableV1Dto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal CurrentStock { get; set; }
        public bool showControl { get; set; } = false;
    }

}
