using Domain.ApiResponse;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using LanguageExt;
using LanguageExt.TypeClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using POSIMSWebApi.Application.Dtos.Inventory;
using POSIMSWebApi.Application.Dtos.Pagination;
using POSIMSWebApi.Application.Dtos.Sales;
using POSIMSWebApi.Application.Interfaces;
using POSIMSWebApi.QueryExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks.Dataflow;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace POSIMSWebApi.Application.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;
        private readonly string _cacheKey = "Inventory";
        private readonly string _allInventory = "AllInventory";
        private readonly CancellationTokenSource _cts = new();
        private static readonly SemaphoreSlim sempahore = new SemaphoreSlim(1, 1);
        public InventoryService(IUnitOfWork unitOfWork, IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
        }

        //public async Task<ApiResponse<PaginatedResult<GetInventoryDto>>> GetAllInventory(InventoryFilter input)
        //{
        //    if (!_memoryCache.TryGetValue(_allInventory, out PaginatedResult<GetInventoryDto> result))
        //    {
        //        try
        //        {
        //            await sempahore.WaitAsync(); 

        //            if (!_memoryCache.TryGetValue(_allInventory, out result))
        //            {
        //                var groupedInventory = (from invBegD in _unitOfWork.InventoryBeginningDetails.GetQueryable()
        //                                        join invBeg in _unitOfWork.InventoryBeginning.GetQueryable()
        //                                        on invBegD.InventoryBeginningId equals invBeg.Id

        //                                        join prod in _unitOfWork.Product.GetQueryable()
        //                                        on invBegD.ProductId equals prod.Id into prodGroup
        //                                        from prod in prodGroup.DefaultIfEmpty()

        //                                        join recv in _unitOfWork.StocksReceiving.GetQueryable()
        //                                        on invBegD.InventoryBeginningId equals recv.InventoryBeginningId into recvGroup
        //                                        from recv in recvGroup.DefaultIfEmpty()

        //                                        join salesHeader in _unitOfWork.SalesHeader.GetQueryable()
        //                                        on invBegD.InventoryBeginningId equals salesHeader.InventoryBeginningId into salesHeaderGroup
        //                                        from salesHeader in salesHeaderGroup.DefaultIfEmpty()

        //                                        join salesDetail in _unitOfWork.SalesDetail.GetQueryable()
        //                                        on new { SalesHeaderId = salesHeader.Id, ProductId = prod.Id }
        //                                        equals new { SalesHeaderId = salesDetail.SalesHeaderId, ProductId = salesDetail.ProductId } into salesDetailGroup
        //                                        from salesDetail in salesDetailGroup.DefaultIfEmpty()

        //                                        where invBeg.Status == InventoryStatus.Closed

        //                                        group new { invBegD, recv, salesDetail } // ✅ Corrected grouping
        //                                        by new
        //                                        {
        //                                            InventoryId = invBegD.InventoryBeginningId,
        //                                            ProductId = invBegD.ProductId,
        //                                            ProductName = prod.Name,
        //                                            InventoryOpened = invBeg.CreationTime,
        //                                            InventoryClosed = invBeg.TimeClosed
        //                                        } into groupedData

        //                                        select new GetInventoryDto
        //                                        {
        //                                            InventoryId = groupedData.Key.InventoryId,
        //                                            ProductName = groupedData.Key.ProductName,
        //                                            InventoryBegTime = groupedData.Key.InventoryOpened,
        //                                            InventoryEndTime = groupedData.Key.InventoryClosed,

        //                                            BegQty = groupedData.Sum(x => x.invBegD != null ? x.invBegD.Qty : 0m),
        //                                            ReceivedQty = groupedData.Sum(x => x.recv != null ? x.recv.Quantity : 0m),
        //                                            SalesQty = groupedData.Sum(x => x.salesDetail != null ? x.salesDetail.Quantity : 0m)
        //                                        });
        //                var paginatedResult = await groupedInventory
        //                    .WhereIf(input.MinCreationTime is null, e => e.InventoryBegTime == input.MinCreationTime)
        //                    .WhereIf(input.MaxClosedTime is null, e => e.InventoryEndTime == input.MaxClosedTime)
        //                    .OrderByDescending(e => e.InventoryEndTime)
        //                    .ToPaginatedResult(input.PageNumber, input.PageSize)
        //                    .ToListAsync();

        //                var totalCount = await groupedInventory.CountAsync();



        //                result = new PaginatedResult<GetInventoryDto>(paginatedResult, totalCount, (int)input.PageNumber, (int)input.PageSize);

        //                var cacheOptions = new MemoryCacheEntryOptions()
        //                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
        //                    .AddExpirationToken(new CancellationChangeToken(_cts.Token))
        //                    .SetSize(1);

        //                _memoryCache.Set(_allInventory, result, cacheOptions);
        //            }
        //        }
        //        finally
        //        {
        //            sempahore.Release();
        //        }
        //    }
        //    // Current Inventory

        //    if (result.TotalCount <= 0) ApiResponse<PaginatedResult<GetInventoryDto>>.Fail(new ArgumentNullException("Error! Current Stocks can't be generated", nameof(result)).ToString());
        //    return ApiResponse<PaginatedResult<GetInventoryDto>>.Success(result);
        //}

        private DateTimeOffset ConvertToUTC8(DateTimeOffset? date)
        {
            if(date is null)
            {
                return DateTimeOffset.MinValue;
            }
            var notnullDate = (DateTimeOffset)date;
            var result = notnullDate.ToOffset(TimeSpan.FromHours(8));
            return result;
        }

        public async Task<ApiResponse<PaginatedResult<GetInventoryDto>>> GetAllInventory(InventoryFilter input)
        {
            try
            {

                var beginningInventoryQuery = from ibd in _unitOfWork.InventoryBeginningDetails.GetQueryable()
                                              join ib in _unitOfWork.InventoryBeginning.GetQueryable() on ibd.InventoryBeginningId equals ib.Id
                                              join p in _unitOfWork.Product.GetQueryable() on ibd.ProductId equals p.Id
                                              where ib.Status == InventoryStatus.Closed
                                              group ibd by new { ibd.ProductId, ibd.InventoryBeginningId } into g
                                              select new GetInventoryDto
                                              {
                                                  InventoryId = g.Key.InventoryBeginningId,
                                                  ProductId = g.Key.ProductId,
                                                  ProductName = g.Select(x => x.ProductFK.Name).FirstOrDefault(),
                                                  InventoryBegTime = g.Select(x => x.InventoryBeginningFk.CreationTime).FirstOrDefault(),
                                                  InventoryEndTime = g.Select(x => x.InventoryBeginningFk.TimeClosed).FirstOrDefault(),
                                                  BegQty = g.Sum(x => x.Qty),
                                                  SalesQty = _unitOfWork.SalesDetail.GetQueryable()
                                                                .Include(e => e.SalesHeaderFk)
                                                                .Where(e => e.SalesHeaderFk.InventoryBeginningId == g.Key.InventoryBeginningId
                                                                && e.ProductId == g.Key.ProductId).Count() != 0 ? _unitOfWork.SalesDetail.GetQueryable()
                                                                .Include(e => e.SalesHeaderFk)
                                                                .Where(e => e.SalesHeaderFk.InventoryBeginningId == g.Key.InventoryBeginningId
                                                                && e.ProductId == g.Key.ProductId).Sum(e => e.Quantity) : 0m,
                                                  ReceivedQty = _unitOfWork.StocksReceiving.GetQueryable().Include(e => e.StocksHeaderFk)
                                                                .Where(e => e.InventoryBeginningId == g.Key.InventoryBeginningId && e.StocksHeaderFk.ProductId == g.Key.ProductId)
                                                                .Count() != 0 ? _unitOfWork.StocksReceiving.GetQueryable().Include(e => e.StocksHeaderFk)
                                                                .Where(e => e.InventoryBeginningId == g.Key.InventoryBeginningId && e.StocksHeaderFk.ProductId == g.Key.ProductId)
                                                                .Sum(e => e.Quantity) : 0m
                                              };

                // Apply Filtering & Pagination
                var paginatedQuery = beginningInventoryQuery
                    .WhereIf(!string.IsNullOrWhiteSpace(input.ProductName), e => e.ProductName.Contains(input.ProductName))
                    .WhereIf(input.MinCreationTime is not null, e => e.InventoryBegTime >= ConvertToUTC8(input.MinCreationTime))
                    .WhereIf(input.MaxClosedTime is not null, e => e.InventoryEndTime <= ConvertToUTC8(input.MaxClosedTime).AddHours(23).AddMinutes(59))
                    .OrderByDescending(e => e.InventoryEndTime)
                    .ToList();


                var totalCount = beginningInventoryQuery.Count();

                var result = new PaginatedResult<GetInventoryDto>(paginatedQuery, totalCount, (int)input.PageNumber, (int)input.PageSize);

                if (result.TotalCount <= 0)
                    return ApiResponse<PaginatedResult<GetInventoryDto>>.Fail("Error! Current Stocks can't be generated");

                return ApiResponse<PaginatedResult<GetInventoryDto>>.Success(result);
            }
            catch (Exception ex )
            {

                throw ex;
            }
        }

        //public async Task<ApiResponse<PaginatedResult<GetInventoryDto>>> GetAllInventory(InventoryFilter input)
        //{
        //    if (!_memoryCache.TryGetValue(_allInventory, out PaginatedResult<GetInventoryDto> result))
        //    {
        //        try
        //        {
        //            await sempahore.WaitAsync();

        //            if (!_memoryCache.TryGetValue(_allInventory, out result))
        //            {
        //                var currentInventory = _unitOfWork.InventoryBeginningDetails
        //                .GetQueryable()
        //                .Include(e => e.InventoryBeginningFk)
        //                .Include(e => e.ProductFK)
        //                .Where(e => e.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Closed)
        //                .WhereIf(input.MinCreationTime != null, e => e.InventoryBeginningFk.CreationTime == input.MinCreationTime)
        //                .WhereIf(input.MaxCreationTime != null, e => e.InventoryBeginningFk.CreationTime == input.MaxCreationTime)
        //                .WhereIf(input.MinClosedTime != null, e => e.InventoryBeginningFk.TimeClosed == input.MinClosedTime)
        //                .WhereIf(input.MaxClosedTime != null, e => e.InventoryBeginningFk.TimeClosed == input.MaxClosedTime);

        //                var getCurrentInventory = await currentInventory
        //                .ToPaginatedResult(input.PageNumber, input.PageSize)
        //                .GroupBy(e => e.InventoryBeginningId)
        //                .Select(g => new
        //                {
        //                    InventoryBegId = g.Key,
        //                    Products = g.GroupBy(e => new
        //                    {
        //                        e.ProductId,
        //                        e.ProductFK.Name
        //                    })
        //                    .Select(pg => new
        //                    {
        //                        ProductId = pg.Key.ProductId,
        //                        ProductName = pg.Key.Name,
        //                        CreationTime = g.Select(e => e.InventoryBeginningFk.CreationTime),
        //                        TimeClosed = g.Select(e => e.InventoryBeginningFk.TimeClosed),
        //                        TotalQuantity = g.Sum(e => e.Qty)
        //                    }).ToList()
        //                }).ToListAsync();

        //                //var getCurrentInventory = await currentInventory
        //                //.ToPaginatedResult(input.PageNumber, input.PageSize)
        //                //.GroupBy(e => new
        //                //{
        //                //    e.ProductFK.Id,
        //                //    e.ProductFK.Name,
        //                //    InventoryBeginningId = e.InventoryBeginningId
        //                //})
        //                //.Select(g => new
        //                //{
        //                //    InventoryBeginningId = g.Key.InventoryBeginningId,
        //                //    ProductId = g.Key.Id,
        //                //    ProductName = g.Key.Name,
        //                //    CreationTime = g.Select(e => e.CreationTime),
        //                //    TotalQuantity = g.Sum(e => e.Qty)
        //                //}).ToListAsync();

        //                var currentInventoryCount = await currentInventory.CountAsync();

        //                if (currentInventoryCount <= 0)
        //                {
        //                    throw new ArgumentNullException("Invalid Action! There is no beginning inventory", nameof(getCurrentInventory));
        //                }

        //                var receivedStocks = _unitOfWork.StocksReceiving.GetQueryable()
        //                    .Include(e => e.StocksHeaderFk)
        //                    .ThenInclude(e => e.ProductFK)
        //                    .Include(e => e.InventoryBeginningFk)
        //                    .Where(e => e.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Closed)
        //                    .GroupBy(e => e.InventoryBeginningId)
        //                    .Select(g => new GetAllInventoryDto
        //                    {
        //                        InventoryBeginningId = g.Key,
        //                        ProductInventoryDtos = g.GroupBy(
        //                            e => e.StocksHeaderFk.ProductId
        //                            ).Select(pg => new ProductInventoryDto
        //                            {
        //                                ProductId = pg.Key,
        //                                TotalQuantity = pg.Sum(e => e.Quantity)
        //                            }).ToList()
        //                    });

        //                var salesDetails = _unitOfWork.SalesDetail.GetQueryable()
        //                    .Include(e => e.SalesHeaderFk.InventoryBeginningFk)
        //                    .Where(e => e.SalesHeaderFk.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Closed)
        //                    .GroupBy(e => e.SalesHeaderFk.InventoryBeginningId)
        //                    .Select(g => new GetAllInventoryDto
        //                    {
        //                        InventoryBeginningId = (Guid)g.Key,
        //                        ProductInventoryDtos =
        //                        g.GroupBy(pg => pg.ProductId)
        //                        .Select(pg => new ProductInventoryDto
        //                        {
        //                            ProductId = pg.Key,
        //                            TotalQuantity = pg.Sum(e => e.Quantity)
        //                        }).ToList()
        //                    });

        //                // Received Stocks
        //                //var receivedStocks = _unitOfWork.StocksReceiving.GetQueryable()
        //                //    .Include(e => e.StocksHeaderFk)
        //                //    .ThenInclude(e => e.ProductFK)
        //                //    .Include(e => e.InventoryBeginningFk)
        //                //    .Where(e => e.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Closed)
        //                //    .GroupBy(e => new
        //                //    {
        //                //        ProductId = e.StocksHeaderFk.ProductId,
        //                //        InventoryId = e.InventoryBeginningId
        //                //    })
        //                //    .Select(group => new
        //                //    {
        //                //        ProductId = group.Key.ProductId,
        //                //        InventoryBeginningId = group.Key.InventoryId,
        //                //        TotalQuantity = group.Sum(e => e.Quantity)
        //                //    });



        //                // Sales Details
        //                //var salesDetails = _unitOfWork.SalesDetail.GetQueryable()
        //                //    .Include(e => e.SalesHeaderFk.InventoryBeginningFk)
        //                //    .Where(e => e.SalesHeaderFk.InventoryBeginningFk.Status == Domain.Enums.InventoryStatus.Closed)
        //                //    .GroupBy(e => new GetAllInventoryDto
        //                //    {
        //                //        InventoryBeginningId = e.SalesHeaderFk.InventoryBeginningId
        //                //    })
        //                //    .Select(g => new
        //                //    {
        //                //        InventoryBeginningId = g.Key.InventoryBeginningId,
        //                //        ProductId = g.Key.ProductId,
        //                //        TotalQuantity = g.Sum(e => e.Quantity)
        //                //    });

        //                var join = (from currInv in getCurrentInventory
        //                        join recv in receivedStocks on currInv.InventoryBegId equals recv.InventoryBeginningId into recvGroup
        //                        from recv in recvGroup.DefaultIfEmpty()
        //                        join sales in salesDetails on currInv.InventoryBegId equals sales.InventoryBeginningId into salesGroup
        //                        from sales in salesGroup.DefaultIfEmpty()
        //                        select new GetInventoryDto
        //                        {
        //                            InventoryId = currInv.InventoryBegId,
        //                            ProductName = currInv.Products.Select(e => e.ProductName).FirstOrDefault(),
        //                            ReceivedQty = recv != null ? recv.ProductInventoryDtos.Select(e => e.TotalQuantity).FirstOrDefault() : 0,
        //                            SalesQty = sales != null ? sales.ProductInventoryDtos.Select(e => e.TotalQuantity).FirstOrDefault() : 0,
        //                            BegQty = currInv.Products.Select(e => e.TotalQuantity).FirstOrDefault(),
        //                        }).ToList();

        //                result = new PaginatedResult<GetInventoryDto>(join, currentInventoryCount, (int)input.PageNumber, (int)input.PageSize);

        //                var cacheOptions = new MemoryCacheEntryOptions()
        //                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
        //                    .AddExpirationToken(new CancellationChangeToken(_cts.Token))
        //                    .SetSize(1);

        //                _memoryCache.Set(_allInventory, result, cacheOptions);
        //            }
        //        }
        //        finally
        //        {
        //            sempahore.Release();
        //        }
        //    }
        //    // Current Inventory

        //    if (result.TotalCount <= 0) ApiResponse<PaginatedResult<GetInventoryDto>>.Fail(new ArgumentNullException("Error! Current Stocks can't be generated", nameof(result)).ToString());
        //    return ApiResponse<PaginatedResult<GetInventoryDto>>.Success(result);
        //}

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
        
        public async Task<Guid> CreateOrGetInventoryBeginning()
        {
            var checkIfInventoryIsOpen = await _unitOfWork.InventoryBeginning.FirstOrDefaultAsync(e => e.Status == InventoryStatus.Open);
            if (checkIfInventoryIsOpen is not null)
            {
                return checkIfInventoryIsOpen.Id;
            }
            var newInventory = new InventoryBeginning
            {
                Status = InventoryStatus.Open,
                Id = Guid.NewGuid(),
            };

            await _unitOfWork.InventoryBeginning.AddAsync(newInventory);
            _unitOfWork.Complete();
            _memoryCache.Remove(_cacheKey);
            _memoryCache.Remove(_allInventory);
            return newInventory.Id;
        }

        public async Task<ApiResponse<string>> CloseInventory()
        {
            try
            {
                var getCurrentOpenedInventory = await _unitOfWork.InventoryBeginning
                    .FirstOrDefaultAsync(e => e.Status == InventoryStatus.Open);

                if (getCurrentOpenedInventory is null)
                    return ApiResponse<string>.Fail("Error! Inventory can't be closed because there are no open records.");
                var currentOpenedInvId = getCurrentOpenedInventory.Id;

                var received = await _unitOfWork.StocksReceiving
                    .GetQueryable()
                    .Include(e => e.StocksHeaderFk)
                    .Where(e => e.InventoryBeginningId == currentOpenedInvId)
                    .GroupBy(e => new
                    {
                        e.StocksHeaderFk.ProductId
                    })
                    .Select(e => new
                    {
                        ProductId = e.Key.ProductId,
                        TotalQuantity = e.Sum(x => x.Quantity)
                    })
                    .ToListAsync();

                //var sales = await _context.SalesHeaders.Include(e => e.SalesDetails).Include(e => e.InventoryBeginningFk)
                //    .Where(e => e.InventoryBeginningFk.Status == InventoryStatus.Open).GroupBy(e => e.SalesDetails)

                var salesDetails = await _unitOfWork.SalesDetail.GetQueryable().Include(e => e.SalesHeaderFk)
                    .Where(e => e.SalesHeaderFk.InventoryBeginningId == currentOpenedInvId
                    )
                    .GroupBy(e => e.ProductId)
                    .Select(e => new
                    {
                        ProductId = e.Key,
                        TotalQuantity = e.Sum(e => e.Quantity)
                    }).ToListAsync();

                //get inventory beginning details
                var inventoryDetails = await _unitOfWork.InventoryBeginningDetails.GetQueryable()
                    .Where(e => e.InventoryBeginningId == currentOpenedInvId).Select(e => new
                    {
                        ProductId = e.ProductId,
                        Quantity = e.Qty
                    }).ToListAsync();
                //close existing inventory
                getCurrentOpenedInventory.Status = InventoryStatus.Closed;
                getCurrentOpenedInventory.TimeClosed = DateTime.UtcNow;

                //create new inventory beginning
                InventoryBeginning newInventory = new InventoryBeginning
                {
                    Id = Guid.NewGuid(),
                    Status = 0,
                    CreationTime = DateTime.UtcNow,
                };

                //join 3 tables
                var join = (from i in inventoryDetails
                            join r in received
                            on i.ProductId equals r.ProductId into jir
                            from ir in jir.DefaultIfEmpty() // Left join with received
                            join s in salesDetails
                            on i.ProductId equals s.ProductId into jirs
                            from irs in jirs.DefaultIfEmpty() // Left join with salesDetails
                            select new InventoryBeginningDetails
                            {
                                ProductId = i.ProductId, // Use i.ProductId directly as it always exists
                                Qty = (i.Quantity + (ir?.TotalQuantity ?? 0)) - (irs?.TotalQuantity ?? 0),
                                InventoryBeginningId = newInventory.Id,
                                CreationTime = DateTime.UtcNow,
                            }).ToList();



                //insert to inventory beginning details
                await _unitOfWork.InventoryBeginning.AddAsync(newInventory);
                await _unitOfWork.InventoryBeginningDetails.AddRangeAsync(join);

                _unitOfWork.Complete();
                _memoryCache.Remove(_cacheKey);

                return ApiResponse<string>.Success("Success!");
            }
            catch (Exception ex)
            {

                throw ex;
            }

            //get sales and receiving
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
            _memoryCache.Remove(_cacheKey);
            _memoryCache.Remove(_allInventory);
            return "Success!";
        }

        
    }
}
