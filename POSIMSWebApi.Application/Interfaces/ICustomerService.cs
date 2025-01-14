using Domain.ApiResponse;
using POSIMSWebApi.Application.Dtos.Customer;
using POSIMSWebApi.Application.Dtos.Pagination;
using POSIMSWebApi.Application.Dtos.ProductDtos;

namespace POSIMSWebApi.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<ApiResponse<PaginatedResult<CustomerDropDownDto>>> CustomerDropDown(GenericSearchParams input);
    }
}