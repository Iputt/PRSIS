using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS.Model
{
    public class AppDbContext
    {
        public string ConnectionString { get; set; }
        public AppDbContext(string connectionString)
        {
            ConnectionString = connectionString;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public async Task<List<T>> GetAllInfo<T>() where T : class, new()
        {
            List<T> list = new List<T>();
            Type type = typeof(T);
            //在指定 String 数组的每个元素之间串联指定的分隔符 String，从而产生单个串联的字符串
            string columString = string.Join(",", type.GetProperties().Select(x => x.Name));
            string sql = $"SELECT {columString} FROM {type.Name}";
            await Task.Run(() =>
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    MySqlCommand command = new MySqlCommand(sql, conn);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T t = Activator.CreateInstance<T>();
                            //动态绑定值
                            foreach (var prop in type.GetProperties())
                            {
                                if (prop.Name == "Getder") continue; 
                                prop.SetValue(t, reader[prop.Name].ToString());
                            }
                            list.Add(t);
                        }
                    }
                }
            });
            return list;
        }

        //public DbSet<Login> Logins { get; set; }

        //public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        //{

        //}
    }
}
