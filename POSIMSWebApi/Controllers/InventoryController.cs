using Domain.ApiResponse;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POSIMSWebApi.Application.Dtos.Inventory;
using POSIMSWebApi.Application.Dtos.Pagination;
using POSIMSWebApi.Application.Interfaces;
using POSIMSWebApi.Authentication;

namespace POSIMSWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IInventoryService _inventoryService;
        public InventoryController(IInventoryService inventoryService, IUnitOfWork unitOfWork)
        {
            _inventoryService = inventoryService;
            _unitOfWork = unitOfWork;
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory + "," + UserRole.Owner)]
        [HttpGet("GetCurrentStocks")]
        public async Task<ActionResult<ApiResponse<List<CurrentInventoryDto>>>> GetCurrentStocks()
        {
            try
            {
               
                var data = await _inventoryService.GetCurrentStocks();
                return Ok(data);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }

        //[Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory)]
        [HttpGet("GetAllInventory")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<GetInventoryDto>>>> GetAllInventory([FromQuery]InventoryFilter input)
        {
            try
            {

                var data = await _inventoryService.GetAllInventory(input);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("BeginningEntry")]
        //[AllowAnonymous]
        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory)]
        public async Task<IActionResult> BeginningEntry(CreateBeginningEntryDto input)
        {
            var data = await _inventoryService.BeginningEntry(input);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            return Ok(data);
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory)]
        [HttpPost("CloseInventory")]
        public async Task<ActionResult<ApiResponse<string>>> CloseInventory()
        {
            var data = await _inventoryService.CloseInventory();
            if(data is null)
            {
                return ApiResponse<string>.Fail("Failed");
            }
            return Ok(data);
        }

        [HttpGet("GetCurrentStocksByProduct/{id}")]
        public async Task<ActionResult<ApiResponse<CurrentInventoryV1Dto>>> GetCurrentStocksByProduct(int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var productCurrentInventory = await _inventoryService.GetCurrentStocksByProduct(id);
            if(!productCurrentInventory.IsSuccess)
            {
                return BadRequest(productCurrentInventory);
            }
            return Ok(productCurrentInventory);
        }
    }
}
