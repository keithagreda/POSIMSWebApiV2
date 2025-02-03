
using Domain.ApiResponse;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using POSIMSWebApi;

namespace DataAccess.EFCore.Repositories
{
    public class InventoryBeginningRepository : GenericRepository<InventoryBeginning>, IInventoryBeginningRepository
    {
        public InventoryBeginningRepository(ApplicationContext context) : base(context)
        {
        }

       
    }
}
