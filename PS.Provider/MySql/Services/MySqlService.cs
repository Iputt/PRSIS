using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;

namespace PS.Provider
{
    public sealed class MySqlService
    {
        public static string _connectionString;

        public MySqlService(string conn)
        {
            _connectionString = conn;
        }

        #region ExecuteMySqlScript

        public static int ExecuteMySqlScript(string path)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand mySqlCommand = new MySqlCommand())
                {
                    using (StreamReader streamReader = new StreamReader(path, System.Text.Encoding.UTF8))
                    {
                        mySqlCommand.Connection = mySqlConnection;
                        mySqlCommand.CommandText = streamReader.ReadToEnd();
                        mySqlConnection.Open(); 
                        return mySqlCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        #endregion ExecuteMySqlScript

        #region ExecuteNonQuery

        public static int ExecuteNonQuery(string commandText, params MySqlParameter[] commandParameters)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand mySqlCommand = new MySqlCommand(commandText, mySqlConnection))
                {
                    if (commandParameters != null)
                    {
                        mySqlCommand.Parameters.Clear();
                        mySqlCommand.Parameters.AddRange(commandParameters);
                    }

                    mySqlConnection.Open();

                    return mySqlCommand.ExecuteNonQuery();
                }
            }
        }

        #endregion ExecuteNonQuery

        #region GetEntities

        public static List<T> GetEntities<T>(string commandText, params MySqlParameter[] commandParameters)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand mySqlCommand = new MySqlCommand(commandText, mySqlConnection))
                {
                    mySqlConnection.Open();
                    if (commandParameters != null)
                    {
                        mySqlCommand.Parameters.Clear();
                        mySqlCommand.Parameters.AddRange(commandParameters);
                    }

                    using (MySqlDataReader dataReader = mySqlCommand.ExecuteReader())
                    {
                        List<T> list = new List<T>();

                        while (dataReader.Read())
                        {
                            List<string> field = new List<string>(dataReader.FieldCount);

                            for (int i = 0; i < dataReader.FieldCount; i++)
                            {
                                field.Add(dataReader.GetName(i).ToLower());
                            }

                            T model = Activator.CreateInstance<T>();

                            foreach (PropertyInfo property in model.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance))
                            {
                                if (field.Contains(property.Name.ToLower()))
                                {
                                    property.SetValue(model, Convert.ChangeType(dataReader[property.Name], property.PropertyType), null);
                                }
                            }

                            list.Add(model);
                        }

                        return list;
                    }
                }
            }
        }

        #endregion GetEntities

        #region GetEntity

        public static T GetEntity<T>(string commandText, params MySqlParameter[] commandParameters)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand mySqlCommand = new MySqlCommand(commandText, mySqlConnection))
                {
                    mySqlConnection.Open();

                    if (commandParameters != null)
                    {
                        mySqlCommand.Parameters.Clear();
                        mySqlCommand.Parameters.AddRange(commandParameters);
                    }

                    using (MySqlDataReader dataReader = mySqlCommand.ExecuteReader())
                    {
                        T model = Activator.CreateInstance<T>();

                        if (dataReader.Read())
                        {
                            List<string> field = new List<string>(dataReader.FieldCount);

                            for (int i = 0; i < dataReader.FieldCount; i++)
                            {
                                field.Add(dataReader.GetName(i).ToLower());
                            }

                            foreach (PropertyInfo property in model.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance))
                            {
                                if (field.Contains(property.Name.ToLower()))
                                {
                                    property.SetValue(model, Convert.ChangeType(dataReader[property.Name], property.PropertyType), null);
                                }
                            }
                        }

                        return model;
                    }
                }
            }
        }

        #endregion GetEntity

        #region ExecuteReader

        public static MySqlDataReader ExecuteReader(string commandText, params MySqlParameter[] commandParameters)
        {
            MySqlConnection mySqlConnection = new MySqlConnection(_connectionString);
            MySqlCommand mySqlCommand = new MySqlCommand(commandText, mySqlConnection);

            if (commandParameters != null)
            {
                mySqlCommand.Parameters.Clear();
                mySqlCommand.Parameters.AddRange(commandParameters);
            }

            mySqlConnection.Open();

            return mySqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
        }

        #endregion ExecuteReader

        #region ExecuteScalar

        public static object ExecuteScalar(string commandText, params MySqlParameter[] commandParameters)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand mySqlCommand = new MySqlCommand(commandText, mySqlConnection))
                {
                    if (commandParameters != null)
                    {
                        mySqlCommand.Parameters.Clear();
                        mySqlCommand.Parameters.AddRange(commandParameters);
                    }

                    mySqlConnection.Open();

                    return mySqlCommand.ExecuteScalar();
                }
            }
        }

        #endregion ExecuteScalar

        #region ExecuteDataSet

        public static DataSet ExecuteDataSet(string commandText, params MySqlParameter[] commandParameters)
        {
            using (MySqlDataAdapter mySqlDataAdapter = new MySqlDataAdapter(commandText, _connectionString))
            {
                DataSet dataSet = new DataSet();

                if (commandParameters != null)
                {
                    mySqlDataAdapter.SelectCommand.Parameters.Clear();
                    mySqlDataAdapter.SelectCommand.Parameters.AddRange(commandParameters);
                }

                mySqlDataAdapter.Fill(dataSet);

                return dataSet;
            }
        }

        #endregion ExecuteDataSet

        #region ExecuteDataTable

        public static DataTable ExecuteDataTable(string commandText, params MySqlParameter[] commandParameters)
        {
            using (MySqlDataAdapter mySqlDataAdapter = new MySqlDataAdapter(commandText, _connectionString))
            {
                DataTable dataTable = new DataTable();

                if (commandParameters != null)
                {
                    mySqlDataAdapter.SelectCommand.Parameters.Clear();
                    mySqlDataAdapter.SelectCommand.Parameters.AddRange(commandParameters);
                }

                mySqlDataAdapter.Fill(dataTable);

                return dataTable;
            }
        }

        #endregion ExecuteDataTable

        #region ExecuteTransaction

        public static int ExecuteTransaction(List<string> list)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand mySqlCommand = new MySqlCommand())
                {
                    mySqlConnection.Open();

                    MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

                    mySqlCommand.Connection = mySqlConnection;
                    mySqlCommand.Transaction = mySqlTransaction;

                    try
                    {
                        int result = 0;

                        foreach (var item in list)
                        {
                            mySqlCommand.CommandText = item;

                            result += mySqlCommand.ExecuteNonQuery();
                        }

                        mySqlTransaction.Commit();

                        return result;
                    }
                    catch (System.Exception)
                    {
                        mySqlTransaction.Rollback();

                        return 0;
                    }
                }
            }
        }

        public static int ExecuteTransaction(List<KeyValuePair<string, MySqlParameter[]>> list)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand mySqlCommand = new MySqlCommand())
                {
                    mySqlConnection.Open();

                    MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

                    mySqlCommand.Connection = mySqlConnection;
                    mySqlCommand.Transaction = mySqlTransaction;

                    try
                    {
                        int result = 0;

                        foreach (var item in list)
                        {
                            mySqlCommand.CommandText = item.Key;
                            mySqlCommand.Parameters.Clear();
                            mySqlCommand.Parameters.AddRange(item.Value);

                            result += mySqlCommand.ExecuteNonQuery();
                        }

                        mySqlTransaction.Commit();

                        return result;
                    }
                    catch (System.Exception)
                    {
                        mySqlTransaction.Rollback();

                        return 0;
                    }
                }
            }
        }

        #endregion ExecuteTransaction
    }
}
