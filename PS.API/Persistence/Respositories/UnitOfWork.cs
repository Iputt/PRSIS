using PS.API.Domain.Repositories;
using PS.API.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Persistence.Respositories
{
    public class UnitOfWork : IUnitOfWork
    {
		private readonly AppDbContext _context;


		public UnitOfWork(AppDbContext context)
		{
			_context = context;
		}


		public async Task CompleteAsync()
		{
			await _context.SaveChangesAsync();
		}
	}
}
