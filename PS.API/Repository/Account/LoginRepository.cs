using MySql.Data.MySqlClient;
using PS.External.Model;
using PS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API
{
    public class LoginRepository : ILogin
    {
        private readonly AppDbContext _dbContext;

        public LoginRepository(IServiceProvider serviceProvider)
        {
            _dbContext = serviceProvider.GetService(typeof(AppDbContext)) as AppDbContext;
        }

        public string ConnectionString { get; set; }
        public Task<Login> Add(Login login)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Login>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<Login> GetById(int id)
        {
            throw new NotImplementedException();
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
        public List<RLoginDto> GetAllUser()
        {
            List<RLoginDto> list = new List<RLoginDto>();
            //连接数据库
            using (MySqlConnection msconnection = GetConnection())
            {
                msconnection.Open();
                //查找数据库里面的表
                MySqlCommand mscommand = new MySqlCommand("select name,account,authentication_string from user", msconnection);
                using (MySqlDataReader reader = mscommand.ExecuteReader())
                {
                    //读取数据
                    while (reader.Read())
                    {
                        list.Add(new RLoginDto()
                        {
                            Name = reader.GetString("name"),
                            Account = reader.GetString("account")
                        });
                    }
                }
            }
            return list;
        }

        public async Task<List<Login>> GetLoginsAsync()
        {
            return await _dbContext.GetAllInfo<Login>();
        }
    }
}
