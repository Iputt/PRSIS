using PS.API.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Domain.Repositories
{
    public interface ICategoryRespository
    {
        Task<IEnumerable<Category>> ListAsync();
    }
}
