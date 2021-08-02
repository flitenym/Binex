using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using SharedLibrary.AbstractClasses;
using SharedLibrary.Helper;
using System.Threading.Tasks;

namespace SharedLibrary.LocalDataBase
{
    public static class SQLExecutor
    {
        private static string loadConnectionString = ConfigurationManager.ConnectionStrings["LocalDataBase"].ConnectionString;

        public static string LoadConnectionString
        {
            get
            {
                return loadConnectionString;
            }
            set
            {
                loadConnectionString = value;
            }
        }

        public static async Task<DataTable> SelectExecutorAsync(Type type, string tableName, string param = default)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        return (await slc.QueryAsync(type, $"SELECT * FROM {tableName} {param}")).ToList().ToDataTable(type) ?? new DataTable(); 
                    }
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                    return new DataTable();
                }
            });
        }

        public static async Task<List<T>> SelectExecutorAsync<T>(string tableName, string param = default)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        return (await slc.QueryAsync<T>($"SELECT * FROM {tableName} {param}")).ToList();
                    }
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                    return new List<T>();
                }
            });
        }

        public static async Task<List<T>> SelectExecutorAsync<T>(string query)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        return (await slc.QueryAsync<T>(query)).ToList();
                    }
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                    return new List<T>();
                }
            });
        }

        public static async Task DeleteExecutorAsync(string tableName, List<int> IDs)
        {
            await Task.Run(async () =>
            {
                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        await slc.ExecuteAsync($"DELETE FROM {tableName} WHERE ID = @ID", IDs.Select(x => new { Id = x }).ToArray());
                    }
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                }
            });
        }

        public static async Task DeleteExecutorAsync(string tableName, string param)
        {
            await Task.Run(async () =>
            {
                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        await slc.ExecuteAsync($"DELETE FROM {tableName} {param}");
                    }
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                }
            });
        }

        public static async Task DeleteExecutorAsync(string tableName)
        {
            await Task.Run(async () =>
            {
                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        await slc.ExecuteAsync($"DELETE FROM {tableName}");
                    } 
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                }
            });
        }

        public static async Task<int> InsertExecutorAsync(ModelClass item, object objData)
        {
            return await Task.Run(async () =>
            {
                string insertQuery = item.InsertQuery(item);

                if (string.IsNullOrEmpty(insertQuery)) return -1;

                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        return await slc.ExecuteScalarAsync<int>(insertQuery, objData);
                    }
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                    return -1;
                }
            });
        }

        public static async Task UpdateExecutorAsync(ModelClass item, Type type, DataRow row, int ID)
        {
            await Task.Run(async () =>
            {
                string updateQuery = item.UpdateQuery(item, ID);

                if (string.IsNullOrEmpty(updateQuery)) return;

                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        await slc.ExecuteAsync(updateQuery, row.ToObject(type));
                    }
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                }
            });
        }

        public static async Task UpdateExecutorAsync(ModelClass item, object obj, int ID)
        {
            await Task.Run(async () =>
            {
                string updateQuery = item.UpdateQuery(item, ID);

                if (string.IsNullOrEmpty(updateQuery)) return;
                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        await slc.ExecuteAsync(updateQuery, obj);
                    }
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                }
            });
        }

        public static async Task QueryExecutorAsync(string query)
        {
            await Task.Run(async () =>
            {
                try
                {
                    using (var slc = new SQLiteConnection(LoadConnectionString))
                    {
                        await slc.OpenAsync();
                        await slc.QueryAsync(query);
                    }
                }
                catch (Exception ex)
                {
                    await HelperMethods.Message(ex.ToString());
                }
            });
        }
    }
}
