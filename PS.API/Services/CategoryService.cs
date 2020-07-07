using PS.API.Domain.Models;
using PS.API.Domain.Repositories;
using PS.API.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRespository _categoryRespository;

        public CategoryService(ICategoryRespository categoryRespository)
        {
            _categoryRespository = categoryRespository;
        }

        public async Task<IEnumerable<Category>> ListAsync()
        {
            return await _categoryRespository.ListAsync();
        }
    }
}
