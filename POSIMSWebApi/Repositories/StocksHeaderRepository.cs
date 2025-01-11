using Domain.Entities;
using Domain.Interfaces;
using POSIMSWebApi;

namespace DataAccess.EFCore.Repositories
{
    public class StocksHeaderRepository : GenericRepository<StocksHeader>, IStocksHeaderRepository
    {
        public StocksHeaderRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
