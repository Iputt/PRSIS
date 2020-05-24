using PS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API
{
    public interface ILogin
    {
        Task<Login> Add(Login login);

        Task<IEnumerable<Login>> GetAll();

        Task<Login> GetById(int id);
    }
}
