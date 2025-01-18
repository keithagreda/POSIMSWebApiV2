namespace POSIMSWebApi.Application.Dtos.ProductDtos
{
    public class ViewSalesParams : GenericSearchParams
    {
        public Guid? SalesHeaderId { get; set; }
    }

}
