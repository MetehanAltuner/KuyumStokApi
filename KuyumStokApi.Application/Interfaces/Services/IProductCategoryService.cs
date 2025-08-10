using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ProductCategories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IProductCategoryService
    {
        Task<ApiResult<List<ProductCategoryDto>>> GetAllAsync();
        Task<ApiResult<ProductCategoryDto>> GetByIdAsync(int id);
        Task<ApiResult<ProductCategoryDto>> CreateAsync(ProductCategoryCreateDto dto);
        Task<ApiResult<bool>> UpdateAsync(int id, ProductCategoryUpdateDto dto);
        Task<ApiResult<bool>> DeleteAsync(int id);
    }
}
