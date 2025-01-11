using Domain.ApiResponse;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSIMSWebApi.Application.Dtos.EntityHistory;
using POSIMSWebApi.Application.Dtos.Pagination;
using POSIMSWebApi.Application.Dtos.ProductDtos;
using POSIMSWebApi.Authentication;
using POSIMSWebApi.QueryExtensions;

namespace POSIMSWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntityHistoryController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public EntityHistoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        [Authorize(Roles = UserRole.Admin)]
        [HttpGet("GetAllEntityHistory")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<EntityHistoryDto>>>> GetAllEntityHistory([FromQuery]GenericSearchParams input)
        {
            var data = _unitOfWork.EntityHistory.GetQueryable();

            var paged = await data
                //.WhereIf(!string.)
                .ToPaginatedResult(input.PageNumber, input.PageSize)
                .OrderByDescending(e => e.ChangeTime).Select(e => new EntityHistoryDto
                {
                    Action = e.Action,
                    Changes = e.Changes != "" ? e.Changes : "-",
                    ChangedBy = e.ChangedBy,
                    ChangeTime = e.ChangeTime.ToString("g"),
                    EntityName = e.EntityName
                }).ToListAsync();

            var totalCount = await data.CountAsync();

            var result = new PaginatedResult<EntityHistoryDto>(paged, totalCount, (int)input.PageNumber, (int)input.PageSize);
            return Ok(ApiResponse<PaginatedResult<EntityHistoryDto>>.Success(result));
        }
    }
}
