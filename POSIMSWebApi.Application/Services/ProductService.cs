using Domain.ApiResponse;
using Domain.Entities;
using Domain.Interfaces;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using POSIMSWebApi.Application.Dtos.ProductDtos;
using POSIMSWebApi.Application.Interfaces;
using POSIMSWebApi.QueryExtensions;
using System.Linq;

namespace POSIMSWebApi.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<IList<ProductWithCategDto>>> GetAllProductsWithCategory()
        {
            var data = await _unitOfWork.Product.GetQueryable().Include(e => e.ProductCategories).Select(e => new ProductWithCategDto
            {
                ProductId = e.Id,
                ProductName = e.Name,
                ProductCategories = e.ProductCategories.Select(e => e.Name).ToList()
            }).AsNoTracking().ToListAsync();

            if(data.Count <= 0)
            {
                return ApiResponse<IList<ProductWithCategDto>>.Fail("Error! No Products With Category Found!");
            }

            return ApiResponse<IList<ProductWithCategDto>>.Success(data, "Request Success!");
        }

        public async Task<ApiResponse<string>> CreateOrEditProduct(CreateProductV1Dto input)
        {
            var exist = _unitOfWork.Product.GetQueryable()
                .WhereIf(input.Id is not null, e => e.Id != input.Id)
                .Where(e => e.Name.Contains(input.Name));

            if(await exist.AnyAsync())
            {
                return ApiResponse<string>.Fail("Invalid action! Product name already exists!");
            }
            var result = new ApiResponse<string>();
            //create
            if (input.Id is null)
            {
               return result = await CreateProduct(input);
            }
            return result = await EditProduct(input);
        }

        private async Task<ApiResponse<string>> EditProduct(CreateProductV1Dto input)
        {
            var query = _unitOfWork.Product.GetQueryable();
            var toEdit = await query.FirstOrDefaultAsync(e => e.Id == input.Id);
            if (toEdit is null)
            {
                return ApiResponse<string>.Fail("Error! Product Not Found!");
            }

            //check if product name is changed
            var generatedProdCode = toEdit.ProdCode;
            var count = 0;
            if (toEdit.Name != input.Name)
            {
                generatedProdCode = GenerateProdCode(input.Name);
                var existingCode = await query.Where(e => e.ProdCode.Contains(generatedProdCode) && e.Id != input.Id).ToListAsync();

                if (existingCode.Count > 0)
                {
                    generatedProdCode = $"{generatedProdCode}{existingCode.Count + 1}";
                }
            }

            toEdit.ProdCode = generatedProdCode;
            toEdit.Name = input.Name;
            toEdit.DaysTillExpiration = input.DaysTillExpiration;

            var categ = await _unitOfWork.ProductCategory.GetQueryable().Where(e => e.Id == input.ProductCategories.Id).ToListAsync();

            if (categ.Count <= 0)
            {
                return ApiResponse<string>.Fail("Product Creation failed input Category doesn't exist!");
            }

            toEdit.ProductCategories = categ;
            toEdit.Price = input.Price;

            _unitOfWork.Complete();

            return ApiResponse<string>.Success($"Successfully edited {input.Name}");
        }

        private async Task<ApiResponse<string>> CreateProduct(CreateProductV1Dto input)
        {
            //data
            //validation
            var query = _unitOfWork.Product.GetQueryable();
            //getCateg
            var generatedProdCode = GenerateProdCode(input.Name);

            var getExistingCode = await query.Where(e => e.ProdCode.Contains(generatedProdCode)).ToListAsync();
            var prodCode = $"{generatedProdCode}";
            if (getExistingCode.Count > 0)
            {
                prodCode = $"{prodCode}{getExistingCode.Count + 1}";
            }

            var categ = await _unitOfWork.ProductCategory.GetQueryable().Where(e => e.Id == input.ProductCategories.Id).ToListAsync();

            if (categ.Count <= 0)
            {
                return ApiResponse<string>.Fail("Product Creation failed input Category doesn't exist!");
            }

            var newProduct = new Product
            {
                Name = input.Name,
                Price = input.Price,
                DaysTillExpiration = input.DaysTillExpiration,
                ProdCode = prodCode,
                ProductCategories = categ
            };

            _unitOfWork.Product.Add(newProduct);
            _unitOfWork.Complete();
            _unitOfWork.Dispose();
            return ApiResponse<string>.Success($"Successfully added {input.Name} in the products list!");
        }

        //private async Task<bool> ValidateProductName(string productName)
        //{
        //    var isExist = await _unitOfWork.Product.FirstOrDefaultAsync(e => e.Name == productName);
        //    if(isExist is not null) return true;
        //    return false;
        //}

        private string GenerateProdCode(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return string.Empty;

            return string.Concat(productName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpper(word[0])));
        }

    }
}
