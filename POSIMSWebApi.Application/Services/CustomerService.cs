using Domain.ApiResponse;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using POSIMSWebApi.Application.Dtos.Customer;
using POSIMSWebApi.Application.Dtos.Pagination;
using POSIMSWebApi.Application.Dtos.ProductDtos;
using POSIMSWebApi.Application.Interfaces;
using POSIMSWebApi.QueryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CustomerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<PaginatedResult<CustomerDropDownDto>>> CustomerDropDown(GenericSearchParams input)
        {
            var query = _unitOfWork.Customer.GetQueryable();
            var data = await query.WhereIf(!string.IsNullOrWhiteSpace(input.FilterText), e => false || e.Name.Contains(input.FilterText))
                .ToPaginatedResult(input.PageNumber, input.PageSize)
                .OrderBy(e => e.Name)
                .Select(e => new CustomerDropDownDto
                {
                    Id = e.Id,
                    CustomerFullName = e.Name
                }).ToListAsync();

            var totalCount = await query.CountAsync();
            var res = new PaginatedResult<CustomerDropDownDto>(data, totalCount, (int)input.PageNumber, (int)input.PageSize);
            return ApiResponse<PaginatedResult<CustomerDropDownDto>>.Success(res);
        }
    }
}
