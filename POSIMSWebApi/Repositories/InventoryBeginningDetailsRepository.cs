using DataAccess.EFCore.Repositories;
using Domain.Entities;
using Domain.Interfaces;

namespace POSIMSWebApi.Repositories
{
    public class InventoryBeginningDetailsRepository : GenericRepository<InventoryBeginningDetails>, IInventoryBeginningDetailsRepository
    {
        public InventoryBeginningDetailsRepository(ApplicationContext context) : base(context)
        {

        }

        public async Task CloseInventoryBeginningDetails()
        {
        }
    }
}
