using Domain.Entities;
using Domain.Interfaces;
using POSIMSWebApi;

namespace DataAccess.EFCore.Repositories
{
    public class StocksReceivingRepository : GenericRepository<StocksReceiving>, IStocksReceivingRepository
    {
        public StocksReceivingRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
