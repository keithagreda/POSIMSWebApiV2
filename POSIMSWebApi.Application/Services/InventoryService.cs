﻿using Domain.ApiResponse;
using Domain.Entities;
using Domain.Interfaces;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using POSIMSWebApi.Application.Dtos.Inventory;
using POSIMSWebApi.Application.Interfaces;
using System.Collections.Generic;

namespace POSIMSWebApi.Application.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        public InventoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ApiResponse<List<CurrentInventoryDto>>> GetCurrentStocks()
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

            if(getCurrentInventory.Count <= 0)
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

            var join = (from currInv in getCurrentInventory
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

            if (join.Count <= 0) ApiResponse<List<CurrentInventoryDto>>.Fail(new ArgumentNullException("Error! Current Stocks can't be generated", nameof(join)).ToString());
            return ApiResponse<List<CurrentInventoryDto>>.Success(join);
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

            if (join.Count <= 0) ApiResponse<List<CurrentInventoryV1Dto>>.Fail(new ArgumentNullException("Error! Current Stocks can't be generated", nameof(join)).ToString());
            return ApiResponse<List<CurrentInventoryV1Dto>>.Success(join);
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
