using Domain.Interfaces;
using POSIMSWebApi.Application.Dtos.EntityHistory;
using POSIMSWebApi.Application.Dtos.Pagination;
using POSIMSWebApi.Application.Dtos.ProductDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Services
{
    public class EntityHistoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        public EntityHistoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
    }
}
