using Domain.ApiResponse;
using POSIMSWebApi.Application.Dtos.Inventory;
using POSIMSWebApi.Application.Dtos.Pagination;

namespace POSIMSWebApi.Application.Interfaces
{
    public interface IInventoryService
    {
        Task<ApiResponse<List<CurrentInventoryDto>>> GetCurrentStocks();
        Task<string> BeginningEntry(CreateBeginningEntryDto input);
        Task<ApiResponse<CurrentInventoryV1Dto>> GetCurrentStocksByProduct(int productId);
        Task<ApiResponse<List<CurrentInventoryV1Dto>>> GetCurrentStocksV1();
        Task<ApiResponse<PaginatedResult<GetInventoryDto>>> GetAllInventory(InventoryFilter input);
        Task<Guid> CreateOrGetInventoryBeginning();
        Task<ApiResponse<string>> CloseInventory();
        string CancelCache();
    }
}