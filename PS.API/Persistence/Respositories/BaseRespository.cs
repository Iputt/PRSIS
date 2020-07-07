using PS.API.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Persistence.Respositories
{
    public abstract class BaseRespository
    {
        protected readonly AppDbContext _context;

        public BaseRespository(AppDbContext context)
        {
            _context = context;
        }
    }
}
