using Domain.ApiResponse;
using Domain.Entities;
using Domain.Interfaces;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using POSIMSWebApi.Application.Dtos.Inventory;
using POSIMSWebApi.Application.Interfaces;
using System.Collections.Generic;

namespace POSIMSWebApi.Application.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;
        private readonly string _cacheKey = "Inventory";
        private readonly CancellationTokenSource _cts = new();
        private static readonly SemaphoreSlim sempahore = new SemaphoreSlim(1, 1);
        public InventoryService(IUnitOfWork unitOfWork, IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
        }

        public async Task<ApiResponse<List<CurrentInventoryDto>>> GetCurrentStocks()
        {
            if(!_memoryCache.TryGetValue(_cacheKey, out List<CurrentInventoryDto> join))
            {

                try
                {
                    await sempahore.WaitAsync();

                    if(!_memoryCache.TryGetValue(_cacheKey, out join))
                    {
                        var getCurrentInventory = await _unitOfWork.InventoryBeginningDetails
                    .GetQueryable()
                    .Include(e => e.InventoryBeginningFk)
                    .Include(e => e.ProductFK)
                    .Where(e => e.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Open)
                    .GroupBy(e => new
                    {
                        e.ProductFK.Id,
                        e.ProductFK.Name,
                    })
                    .Select(g => new
                    {
                        ProductId = g.Key.Id,
                        ProductName = g.Key.Name,
                        CreationTime = g.Select(e => e.CreationTime),
                        TotalQuantity = g.Sum(e => e.Qty)
                    }).ToListAsync();

                            if (getCurrentInventory.Count <= 0)
                            {
                                throw new ArgumentNullException("Invalid Action! There is no beginning inventory", nameof(getCurrentInventory));
                            }

                            // Received Stocks
                            var receivedStocks = _unitOfWork.StocksReceiving.GetQueryable()
                                .Include(e => e.StocksHeaderFk)
                                .ThenInclude(e => e.ProductFK)
                                .Include(e => e.InventoryBeginningFk)
                                .Where(e => e.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Open)
                                .GroupBy(e => e.StocksHeaderFk.ProductId)
                                .Select(group => new
                                {
                                    ProductId = group.Key,
                                    TotalQuantity = group.Sum(e => e.Quantity)
                                }).ToList();

                            // Sales Details
                            var salesDetails = _unitOfWork.SalesDetail.GetQueryable()
                                .Include(e => e.SalesHeaderFk.InventoryBeginningFk)
                                .Where(e => e.SalesHeaderFk.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Open)
                                .GroupBy(e => e.ProductId)
                                .Select(g => new
                                {
                                    ProductId = g.Key,
                                    TotalQuantity = g.Sum(e => e.Quantity)
                                });

                            join = (from currInv in getCurrentInventory
                                    join recv in receivedStocks on currInv.ProductId equals recv.ProductId into recvGroup
                                    from recv in recvGroup.DefaultIfEmpty()
                                    join sales in salesDetails on currInv.ProductId equals sales.ProductId into salesGroup
                                    from sales in salesGroup.DefaultIfEmpty()
                                    select new CurrentInventoryDto
                                    {
                                        ProductName = currInv.ProductName,
                                        ReceivedQty = recv != null ? recv.TotalQuantity : 0,
                                        SalesQty = sales != null ? sales.TotalQuantity : 0,
                                        BegQty = currInv.TotalQuantity,
                                        CurrentStocks = (currInv != null ? currInv.TotalQuantity : 0) + (recv != null ? recv.TotalQuantity : 0) - (sales != null ? sales.TotalQuantity : 0)
                                    }).ToList();

                            var cacheOptions = new MemoryCacheEntryOptions()
                                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                                .AddExpirationToken(new CancellationChangeToken(_cts.Token))
                                .SetSize(1);

                            _memoryCache.Set(_cacheKey, join, cacheOptions);
                    }
                }
                finally 
                {
                    sempahore.Release();
                }
            }
            // Current Inventory

            if (join.Count <= 0) ApiResponse<List<CurrentInventoryDto>>.Fail(new ArgumentNullException("Error! Current Stocks can't be generated", nameof(join)).ToString());
            return ApiResponse<List<CurrentInventoryDto>>.Success(join);
        }

        public string CancelCache()
        {
            try
            {
                _cts.Cancel();
                return "Success!";
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<ApiResponse<List<CurrentInventoryV1Dto>>> GetCurrentStocksV1()
        {
            // Current Inventory
            var getCurrentInventory = await _unitOfWork.InventoryBeginningDetails
                .GetQueryable()
                .Include(e => e.InventoryBeginningFk)
                .Include(e => e.ProductFK)
                .Where(e => e.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Open)
                .GroupBy(e => new
                {
                    e.ProductFK.Id,
                    e.ProductFK.Name,
                })
                .Select(g => new
                {
                    ProductId = g.Key.Id,
                    ProductName = g.Key.Name,
                    CreationTime = g.Select(e => e.CreationTime),
                    TotalQuantity = g.Sum(e => e.Qty)
                }).ToListAsync();

            if (getCurrentInventory.Count <= 0)
            {
                throw new ArgumentNullException("Invalid Action! There is no beginning inventory", nameof(getCurrentInventory));
            }

            // Received Stocks
            var receivedStocks = await _unitOfWork.StocksReceiving.GetQueryable()
                .Include(e => e.StocksHeaderFk)
                .ThenInclude(e => e.ProductFK)
                .Include(e => e.InventoryBeginningFk)
                .Where(e => e.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Open)
                .GroupBy(e => e.StocksHeaderFk.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    TotalQuantity = group.Sum(e => e.Quantity)
                }).ToListAsync();

            // Sales Details
            var salesDetails = await _unitOfWork.SalesDetail.GetQueryable()
                .Include(e => e.SalesHeaderFk.InventoryBeginningFk)
                .Where(e => e.SalesHeaderFk.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Open)
                .GroupBy(e => e.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(e => e.Quantity)
                }).ToListAsync();

            var join = (from currInv in getCurrentInventory
                        join recv in receivedStocks on currInv.ProductId equals recv.ProductId into recvGroup
                        from recv in recvGroup.DefaultIfEmpty()
                        join sales in salesDetails on currInv.ProductId equals sales.ProductId into salesGroup
                        from sales in salesGroup.DefaultIfEmpty()
                        select new CurrentInventoryV1Dto
                        {
                            ProductId = currInv.ProductId,
                            ReceivedQty = recv != null ? recv.TotalQuantity : 0,
                            SalesQty = sales != null ? sales.TotalQuantity : 0,
                            BegQty = currInv.TotalQuantity,
                            CurrentStocks = (currInv != null ? currInv.TotalQuantity : 0) + (recv != null ? recv.TotalQuantity : 0) - (sales != null ? sales.TotalQuantity : 0)
                        }).ToList();


            //if (join.Count <= 0) ApiResponse<List<CurrentInventoryV1Dto>>.Fail(new ArgumentNullException("Error! Current Stocks can't be generated", nameof(join)).ToString());
            return ApiResponse<List<CurrentInventoryV1Dto>>.Success(join);
        }

        /// <summary>
        /// A function that get current stocks by product id
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<ApiResponse<CurrentInventoryV1Dto>> GetCurrentStocksByProduct(int productId)
        {
            try
            {
                // Current Inventory
                var getCurrentInventory = await _unitOfWork.InventoryBeginningDetails
                    .GetQueryable()
                    .Include(e => e.InventoryBeginningFk)
                    .Include(e => e.ProductFK)
                    .Where(e => e.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Open && e.ProductId == productId)
                    .GroupBy(e => new
                    {
                        e.ProductFK.Id,
                        e.ProductFK.Name,
                    })
                    .Select(g => new 
                    {
                        ProductId = g.Key.Id,
                        ProductName = g.Key.Name,
                        CreationTime = g.Select(e => e.CreationTime),
                        TotalQuantity = g.Sum(e => e.Qty)
                    }).FirstOrDefaultAsync();

                if (getCurrentInventory is null)
                {
                    throw new ArgumentNullException("Invalid Action! There is no beginning inventory", nameof(getCurrentInventory));
                }

                // Received Stocks
                var receivedStocks = await _unitOfWork.StocksReceiving.GetQueryable()
                    .Include(e => e.StocksHeaderFk)
                    .ThenInclude(e => e.ProductFK)
                    .Include(e => e.InventoryBeginningFk)
                    .Where(e => e.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Open && e.StocksHeaderFk.ProductId == productId)
                    .GroupBy(e => e.StocksHeaderFk.ProductId)
                    .Select(group => new
                    {
                        ProductId = group.Key,
                        TotalQuantity = group.Sum(e => e.Quantity)
                    }).FirstOrDefaultAsync();


                // Sales Details
                var salesDetails = await _unitOfWork.SalesDetail.GetQueryable()
                    .Include(e => e.SalesHeaderFk.InventoryBeginningFk)
                    .Where(e => e.SalesHeaderFk.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Open && e.ProductId == productId)
                    .GroupBy(e => e.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        TotalQuantity = g.Sum(e => e.Quantity)
                    }).FirstOrDefaultAsync();

                var summary = new CurrentInventoryV1Dto
                {
                    ProductId = getCurrentInventory.ProductId,
                    ReceivedQty = receivedStocks != null ? receivedStocks.TotalQuantity : 0,
                    SalesQty = salesDetails != null ? salesDetails.TotalQuantity : 0,
                    BegQty = getCurrentInventory.TotalQuantity,
                    CurrentStocks = (getCurrentInventory != null ? getCurrentInventory.TotalQuantity : 0) + (receivedStocks != null ? receivedStocks.TotalQuantity : 0)
                    - (salesDetails != null ? salesDetails.TotalQuantity : 0)
                };

                //if (summary is null) return ApiResponse<CurrentInventoryV1Dto>.Fail(new ArgumentNullException("Error! Current Stocks can't be generated", nameof(summary)).ToString());
                return ApiResponse<CurrentInventoryV1Dto>.Success(summary);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// This function can only be used when beginning entry is null
        /// </summary>
        /// <returns></returns>
        public async Task<string> BeginningEntry(CreateBeginningEntryDto input)
        {
            var getCurrentOpenedInventory = _unitOfWork.InventoryBeginning.GetQueryable().Where(e => e.Status == Domain.Enums.InventoryStatus.Open);
            var gcoId = await getCurrentOpenedInventory.Select(e => e.Id).FirstOrDefaultAsync();

            var product = await _unitOfWork.Product.GetQueryable()
                .Where(e => e.Id == input.ProductId).Select(e => e.Id).FirstOrDefaultAsync();

            if(product == 0)
            {
                throw new ArgumentNullException("Error! Product Not Found.", nameof(product));
            }
            var newInventoryDetail = new InventoryBeginningDetails()
            {
                Id = Guid.NewGuid(),
                InventoryBeginningId = gcoId,
                ProductId = product,
                Qty = input.ReceivedQty
            };

            await _unitOfWork.InventoryBeginningDetails.AddAsync(newInventoryDetail);
            _unitOfWork.Complete();
            return "Success!";
        }

        
    }
}
