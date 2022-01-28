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
        private static string loadConnectionString = ConfigurationManager.ConnectionStrings["LocalDataBase"].ConnectionString.Replace("{AppDir}", AppDomain.CurrentDomain.BaseDirectory);

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
        }

        public static async Task<List<T>> SelectExecutorAsync<T>(string tableName, string param = default)
        {
            string query = $"SELECT * FROM {tableName} {param}";

            return await SelectExecutorAsync<T>(query);
        }

        public static async Task<List<T>> SelectExecutorAsync<T>(string query)
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
        }

        public static async Task<T> SelectFirstExecutorAsync<T>(string query)
        {
            try
            {
                using (var slc = new SQLiteConnection(LoadConnectionString))
                {
                    await slc.OpenAsync();
                    return (await slc.QueryAsync<T>(query)).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.ToString());
                return default;
            }
        }

        public static async Task DeleteExecutorAsync(string tableName, List<int> IDs)
        {
            try
            {
                using (var slc = new SQLiteConnection(LoadConnectionString))
                {
                    await slc.OpenAsync();
                    using (var transaction = slc.BeginTransaction())
                    {
                        try
                        {
                            await slc.ExecuteAsync($"DELETE FROM {tableName} WHERE ID = @ID", IDs.Select(x => new { Id = x }).ToArray(), transaction: transaction);
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            await HelperMethods.Message(ex.ToString());
                            transaction.Rollback();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.ToString());
            }
        }

        public static async Task DeleteExecutorAsync(string tableName, string param)
        {
            try
            {
                using (var slc = new SQLiteConnection(LoadConnectionString))
                {
                    await slc.OpenAsync();
                    using (var transaction = slc.BeginTransaction())
                    {
                        try
                        {
                            await slc.ExecuteAsync($"DELETE FROM {tableName} {param}", transaction: transaction);
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            await HelperMethods.Message(ex.ToString());
                            transaction.Rollback();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.ToString());
            }
        }

        public static async Task DeleteExecutorAsync(string tableName)
        {
            try
            {
                using (var slc = new SQLiteConnection(LoadConnectionString))
                {
                    await slc.OpenAsync();
                    using (var transaction = slc.BeginTransaction())
                    {
                        try
                        {
                            await slc.ExecuteAsync($"DELETE FROM {tableName}", transaction: transaction);
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            await HelperMethods.Message(ex.ToString());
                            transaction.Rollback();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.ToString());
            }
        }

        public static async Task<int> InsertExecutorAsync(ModelClass item, object objData)
        {
            string insertQuery = item.InsertQuery(item);

            if (string.IsNullOrEmpty(insertQuery)) return -1;

            try
            {
                using (var slc = new SQLiteConnection(LoadConnectionString))
                {
                    await slc.OpenAsync();
                    using (var transaction = slc.BeginTransaction())
                    {
                        try
                        {
                            int index = await slc.ExecuteScalarAsync<int>(insertQuery, objData, transaction: transaction);
                            transaction.Commit();
                            return index;
                        }
                        catch (Exception ex)
                        {
                            await HelperMethods.Message(ex.ToString());
                            transaction.Rollback();
                            return -1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.ToString());
                return -1;
            }
        }

        public static async Task InsertSeveralExecutorAsync(ModelClass item, List<object> objectsData)
        {
            if (objectsData == null || objectsData.Count == 0)
            {
                return;
            }

            string insertQuery = item.InsertQuery(item, false);

            if (string.IsNullOrEmpty(insertQuery)) return;

            try
            {
                using (var slc = new SQLiteConnection(LoadConnectionString))
                {
                    await slc.OpenAsync();
                    using (var transaction = slc.BeginTransaction())
                    {
                        try
                        {
                            foreach (var objectData in objectsData)
                            {
                                await slc.ExecuteScalarAsync(insertQuery, objectData, transaction: transaction);
                            }
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            await HelperMethods.Message(ex.ToString());
                            transaction.Rollback();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.ToString());
            }
        }

        public static async Task UpdateExecutorAsync(ModelClass item, Type type, DataRow row, int ID)
        {
            string updateQuery = item.UpdateQuery(item, ID);

            if (string.IsNullOrEmpty(updateQuery)) return;

            try
            {
                using (var slc = new SQLiteConnection(LoadConnectionString))
                {
                    await slc.OpenAsync();
                    using (var transaction = slc.BeginTransaction())
                    {
                        try
                        {
                            await slc.ExecuteAsync(updateQuery, row.ToObject(type), transaction: transaction);
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            await HelperMethods.Message(ex.ToString());
                            transaction.Rollback();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.ToString());
            }
        }

        public static async Task UpdateExecutorAsync(ModelClass item, object obj, int ID)
        {
            string updateQuery = item.UpdateQuery(item, ID);

            if (string.IsNullOrEmpty(updateQuery)) return;
            try
            {
                using (var slc = new SQLiteConnection(LoadConnectionString))
                {
                    await slc.OpenAsync();
                    using (var transaction = slc.BeginTransaction())
                    {
                        try
                        {
                            await slc.ExecuteAsync(updateQuery, obj, transaction: transaction);
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            await HelperMethods.Message(ex.ToString());
                            transaction.Rollback();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.ToString());
            }
        }

        public static async Task QueryExecutorAsync(string query)
        {
            try
            {
                using (var slc = new SQLiteConnection(LoadConnectionString))
                {
                    await slc.OpenAsync();
                    using (var transaction = slc.BeginTransaction())
                    {
                        try
                        {
                            await slc.QueryAsync(query, transaction: transaction);
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            await HelperMethods.Message(ex.ToString());
                            transaction.Rollback();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.ToString());
            }
        }
    }
}