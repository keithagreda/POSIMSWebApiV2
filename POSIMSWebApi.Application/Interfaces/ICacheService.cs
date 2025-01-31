namespace POSIMSWebApi.Application.Services
{
    public interface ICacheService
    {
        string RemoveInventoryCache();
        string RemoveProductCache();
        string RemoveProductCategoryCache();
    }
}