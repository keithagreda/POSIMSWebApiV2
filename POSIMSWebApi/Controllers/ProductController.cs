using Domain.ApiResponse;
using Domain.Entities;
using Domain.Interfaces;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSIMSWebApi.Application.Dtos;
using POSIMSWebApi.Application.Dtos.Pagination;
using POSIMSWebApi.Application.Dtos.ProductCategory;
using POSIMSWebApi.Application.Dtos.ProductDtos;
using POSIMSWebApi.Application.Dtos.Sales;
using POSIMSWebApi.Application.Interfaces;
using POSIMSWebApi.Authentication;
using POSIMSWebApi.QueryExtensions;
using System.Linq;

namespace POSIMSWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;
        public ProductController(IUnitOfWork unitOfWork, IProductService productService, IInventoryService inventoryService)
        {
            _unitOfWork = unitOfWork;
            _productService = productService;
            _inventoryService = inventoryService;
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory + "," + UserRole.Cashier)]
        [HttpPost("CreateOrEditProduct")]
        public async Task<ActionResult<ApiResponse<string>>> CreateOrEditProduct(CreateProductV1Dto input)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var result = await _productService.CreateOrEditProduct(input);
                
                return Ok(result);
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory)]
        [HttpPost("DeleteProduct/{prodId}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteProduct(int prodId)
        {
            try
            {
                if(!ModelState.IsValid) return BadRequest(ModelState);
                var product = await _unitOfWork.Product.FirstOrDefaultAsync(prodId);
                if(product is null)
                {
                    return BadRequest(ApiResponse<string>.Fail("Error! Product Not Found!"));
                }

                _unitOfWork.Product.Remove(product);
                _unitOfWork.Complete();
                return Ok(ApiResponse<string>.Success($"Successfully removed {product.Name}!"));
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory + "," + UserRole.Cashier)]
        [HttpGet("GetProducts")]
        public async Task<ActionResult<ApiResponse<IList<ProductV1Dto>>>> GetProducts()
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var data = await _unitOfWork.Product.GetQueryable().Include(e => e.ProductCategories).Select(e => new ProductV1Dto
            {
                DaysTillExpiration = e.DaysTillExpiration,
                Id = e.Id,
                Name = e.Name,
                Price = e.Price,
                ProdCode = e.ProdCode,
                ProductCategoriesDto = e.ProductCategories.Select(e => new ProductCategoryDto
                {
                    Id = e.Id,
                    Name = e.Name
                }).ToList()
            }).ToListAsync();

            
            return Ok(ApiResponse<IList<ProductV1Dto>>.Success(data));
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory + "," + UserRole.Cashier)]
        [HttpGet("GetAllProductsWithCateg")]
        public async Task<ActionResult<ApiResponse<IList<ProductWithCategDto>>>> GetAllProductsWithCateg()
        {
            var data = await _productService.GetAllProductsWithCategory();
            
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                return Ok(data);

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory + "," + UserRole.Cashier)]
        [HttpGet("GetProductWithCateg/{id}")]
        public async Task<IActionResult> GetProductWithCateg(int id)
        {
            var data = await _unitOfWork.Product.GetQueryable().Include(e => e.ProductCategories).FirstOrDefaultAsync(e => e.Id == id);
            if (data is null) throw new ArgumentNullException($"Product with id: \"{id}\" not found!", nameof(data));
            var result = new ProductWithCategDto
            {
                ProductId = data.Id,
                ProductName = data.Name,
                ProductCategories = data.ProductCategories.Select(e => e.Name).ToList() ?? new List<string>()
            };
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory + "," + UserRole.Cashier)]
        [HttpGet("GetProductForEdit/{id}")]
        public async Task<ActionResult<ApiResponse<CreateProductV1Dto>>> GetProductForEdit(int id)
        {
            if (id == 0)
            {
                return ApiResponse<CreateProductV1Dto>.Fail("Invalid action! Id can't be null");
            }
            var data = await _unitOfWork.Product.GetQueryable().Include(e => e.ProductCategories)
                .Select(e => new CreateProductV1Dto
                {
                    Id = e.Id,
                    DaysTillExpiration = e.DaysTillExpiration,
                    Name = e.Name,
                    Price = e.Price,
                    ProductCategories = e.ProductCategories.Select(e => new ProductCategoryDtoV1
                    {
                        Name = e.Name,
                        Id = e.Id
                    }).FirstOrDefault()
                }).FirstOrDefaultAsync();

            if(data is null)
            {
                return ApiResponse<CreateProductV1Dto>.Fail("Error! Product Not Found.");
            }

            return Ok(ApiResponse<CreateProductV1Dto>.Success(data));
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory + "," + UserRole.Cashier)]
        [HttpGet("GetProductsForDropDown")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<GetProductDropDownTableDto>>>> GetProductDropDownTable([FromQuery]GenericSearchParams? input)
        {
            var query = _unitOfWork.Product.GetQueryable();
            var data = await query
                .WhereIf(!string.IsNullOrWhiteSpace(input.FilterText), e => false || e.Name.Contains(input.FilterText))
                .ToPaginatedResult(input.PageNumber, input.PageSize)
                .OrderBy(e => e.Name).Select(e => new GetProductDropDownTableDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Price = e.Price
                }).ToListAsync();

            var totalCount = await query.CountAsync();

            var result = new PaginatedResult<GetProductDropDownTableDto>(data, totalCount, (int)input.PageNumber, (int)input.PageSize);


            return Ok(ApiResponse<PaginatedResult<GetProductDropDownTableDto>>.Success(result));
        }

        //[Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory + "," + UserRole.Cashier)]
        [HttpGet("GetProductsForDropDownV1")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<GetProductDropDownTableV1Dto>>>> GetProductDropDownTableV1([FromQuery] GenericSearchParams? input)
        {
            try
            {
                var query = _unitOfWork.Product.GetQueryable();
                var product = await query
                    .WhereIf(!string.IsNullOrWhiteSpace(input.FilterText), e => false || e.Name.Contains(input.FilterText))
                    .ToPaginatedResult(input.PageNumber, input.PageSize)
                    .Select(e => new GetProductDropDownTableV1Dto
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Price = e.Price
                    })
                    .ToListAsync(); // Ensure it's executed in memory

                var getStocks = await _inventoryService.GetCurrentStocksV1();
                var stocks = getStocks.Data;

                var productStocks =  (from p in product
                                           join s in stocks on p.Id equals s.ProductId
                                           orderby p.Name
                                           select new GetProductDropDownTableV1Dto
                                           {
                                               Id = s.ProductId,
                                               Name = p.Name,
                                               Price = p.Price,
                                               CurrentStock = s.CurrentStocks
                                           }).ToList();

                var totalCount = await query.CountAsync();

                var result = new PaginatedResult<GetProductDropDownTableV1Dto>(productStocks, totalCount, (int)input.PageNumber, (int)input.PageSize);


                return Ok(ApiResponse<PaginatedResult<GetProductDropDownTableV1Dto>>.Success(result));
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [Authorize(Roles = UserRole.Admin + "," + UserRole.Inventory + "," + UserRole.Cashier)]
        [HttpPost("GetProductDetailsForCart")]
        public async Task<ActionResult<ApiResponse<List<CreateSalesDetailV1Dto>>>> GetProductDetailsForCart(List<CreateSalesDetailV1Dto> input)
        {
            var query = _unitOfWork.Product.GetQueryable();
            var inputListOfProducts = input.ToList();
            var productDetails =await query.Where(e => inputListOfProducts.Select(e => e.ProductId).Contains(e.Id)).Select(e => new
            {
                ProductId = e.Id,
                ProductPrice = e.Price,
                Name = e.Name
            }).ToListAsync();


            var leftJoin = (from n in inputListOfProducts
                            join p in productDetails
                            on n.ProductId equals p.ProductId
                            select new CreateSalesDetailV1Dto
                            {
                                ProductId = p.ProductId, // Use n.ProductId when p is null
                                Quantity = n.Quantity,
                                ProductPrice = (p != null ? p.ProductPrice : 0) * n.Quantity, // Handle null pGroup
                                ProductName = p != null ? p.Name : "Unknown Product" // Provide default name
                            }).ToList();


            return Ok(ApiResponse<List<CreateSalesDetailV1Dto>>.Success(leftJoin));
                            
        }
    }
}
