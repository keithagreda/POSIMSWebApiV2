using Domain.ApiResponse;
using Domain.Error;
using LanguageExt.Common;
using POSIMSWebApi.Application.Dtos.StorageLocation;

namespace POSIMSWebApi.Application.Interfaces
{
    public interface IStorageLocationService
    {
        Task<ApiResponse<string>> CreateStorageLocation(CreateOrEditStorageLocationDto input);
    }
}