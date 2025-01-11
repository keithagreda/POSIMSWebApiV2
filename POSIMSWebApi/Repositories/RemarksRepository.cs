using Domain.Entities;
using Domain.Interfaces;
using POSIMSWebApi;

namespace DataAccess.EFCore.Repositories
{
    public class RemarksRepository : GenericRepository<Remarks>, IRemarksRepository
    {
        public RemarksRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
