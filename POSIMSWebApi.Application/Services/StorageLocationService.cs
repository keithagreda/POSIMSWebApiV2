using Domain.ApiResponse;
using Domain.Entities;
using Domain.Interfaces;
using LanguageExt.Common;
using POSIMSWebApi.Application.Dtos.StorageLocation;
using POSIMSWebApi.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Services
{
    public class StorageLocationService : IStorageLocationService
    {
        private readonly IUnitOfWork _unitOfWork;
        public StorageLocationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private async Task<ApiResponse<string>> ValidateStorageLocation(CreateOrEditStorageLocationDto input)
        {
            var isExisting = new StorageLocation();

                

            if (input.Id is not null)
            {
                isExisting = await _unitOfWork.StorageLocation.FirstOrDefaultAsync(e => e.Name == input.Name && e.Id != input.Id);
                if (isExisting != null)
                {
                    var error = new ValidationException("Error! Location already exists, Param: Name");
                    return ApiResponse<string>.Fail(error.ToString());
                }

                return ApiResponse<string>.Success("Success!");
            }
            isExisting = await _unitOfWork.StorageLocation.FirstOrDefaultAsync(e => e.Name == input.Name);
            if (isExisting != null)
            {
                var error = new ValidationException("Error! Location already exists, Param: Name");
                return ApiResponse<string>.Fail(error.ToString());
            }

            return ApiResponse<string>.Success("Success!");
        }
        public async Task<ApiResponse<string>> CreateStorageLocation(CreateOrEditStorageLocationDto input)
        {
            try
            {
                var validation = await ValidateStorageLocation(input);
                if (!validation.IsSuccess)
                {
                    return validation;
                }
                var newStorageLoc = new StorageLocation
                {
                    Name = input.Name,
                    Description = input.Description,
                };

                await _unitOfWork.StorageLocation.AddAsync(newStorageLoc);
                _unitOfWork.Complete();
                return ApiResponse<string>.Success("Success!");
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
    }
}
