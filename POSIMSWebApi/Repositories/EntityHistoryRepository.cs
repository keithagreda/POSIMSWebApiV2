using Domain.Entities;
using Domain.Interfaces;
using POSIMSWebApi;

namespace DataAccess.EFCore.Repositories
{
    public class EntityHistoryRepository : GenericRepository<EntityHistory>, IEntityHistoryRepository
    {
        public EntityHistoryRepository(ApplicationContext context) : base(context)
        {

        }
    }
}
