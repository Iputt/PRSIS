using Microsoft.EntityFrameworkCore;
using PS.API.Domain.Models;
using PS.API.Domain.Repositories;
using PS.API.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Persistence.Respositories
{
    public class CategoryRespository : BaseRespository, ICategoryRespository
    {
        public CategoryRespository(AppDbContext context) : base(context)
        {

        } 

        public async Task<IEnumerable<Category>> ListAsync()
        {
            return await  _context.Categories.ToListAsync();
        }
    }
}
