using Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly string _invKey = "Inventory";
        private readonly string _productKey = "Product";
        private readonly string _productCategory = "ProductCategory";
        public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public string RemoveInventoryCache()
        {
            try
            {
                _logger.LogInformation("Removing Inventory Cache Manually");
                _memoryCache.Remove(_invKey);
                _logger.LogInformation("Inventory Cache Successfully Removed!");
                return "Success!";
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public string RemoveProductCache()
        {
            try
            {
                _logger.LogInformation("Removing Product Cache Manually");
                _memoryCache.Remove(_productKey);
                _logger.LogInformation("Product Cache Successfully Removed!");
                return "Success!";
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public string RemoveProductCategoryCache()
        {
            try
            {
                _logger.LogInformation("Removing Product Category Cache Manually");
                _memoryCache.Remove(_productCategory);
                _logger.LogInformation("Inventory Product Category Successfully Removed!");
                return "Success!";
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
    }
}
