using Domain.Entities;
using Domain.Interfaces;
using POSIMSWebApi;
using POSIMSWebApi.Application.Dtos.ProductStocks;

namespace DataAccess.EFCore.Repositories
{
    public class ProductStocksRepository : GenericRepository<ProductStocks>, IProductStocksRepository
    {
        public ProductStocksRepository(ApplicationContext context) : base(context)
        {
        }

    }
}
