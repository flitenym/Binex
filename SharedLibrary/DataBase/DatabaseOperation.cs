using Dapper;
using SharedLibrary.Helper.StaticInfo;
using SharedLibrary.LocalDataBase;
using SharedLibrary.LocalDataBase.Models;
using System;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using static SharedLibrary.Helper.HelperMethods;

namespace SharedLibrary.DataBase
{
    public static class DatabaseOperation
    {
        public static string connectionString = string.Empty;
        public static bool canGetData = false;

        public static async Task<string> ConnectionStringAsync()
        {
            if (!canGetData)
            {
                using (var slc = new SQLiteConnection(SQLExecutor.LoadConnectionString))
                {
                    await slc.OpenAsync();
                    var connectionStringData = (await slc.QueryAsync<Settings>($"SELECT * FROM {nameof(Settings)} Where Name = '{InfoKeys.ConnectionStringKey}'")).FirstOrDefault();
                    if (connectionStringData != null && !string.IsNullOrEmpty(connectionStringData.Value))
                    {
                        await TryConnection(connectionStringData.Value, connectionStringData);
                        if (canGetData)
                        {
                            connectionString = connectionStringData.Value;
                        }
                    }
                }
            }

            return connectionString;
        }

        public static async Task TryConnection(string ConnectionString, Settings connectionStringData = null)
        {
            try
            {
                connectionString = string.Empty;
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    connection.Close();
                    connectionString = ConnectionString;
                    canGetData = true;
                    await Message("ConnectionString верный");

                    if (connectionStringData == null)
                    {
                        using (var slc = new SQLiteConnection(SQLExecutor.LoadConnectionString))
                        {
                            await slc.OpenAsync();
                            connectionStringData = (await slc.QueryAsync<Settings>($"SELECT * FROM {nameof(Settings)} Where Name = '{InfoKeys.ConnectionStringKey}'")).FirstOrDefault();
                        }

                        if (connectionStringData == null)
                        {
                            connectionStringData = new Settings() { Name = InfoKeys.ConnectionStringKey, Value = connectionString };
                            await SQLExecutor.InsertExecutorAsync(connectionStringData, connectionStringData);
                        }
                    }

                    if (connectionStringData != null && connectionStringData.Value != connectionString)
                    {
                        connectionStringData.Value = connectionString;
                        await SQLExecutor.UpdateExecutorAsync(connectionStringData, connectionStringData, connectionStringData.ID);
                    }
                }
            }
            catch (Exception ex)
            {
                await Message($"ConnectionString неверный, проверьте адрес сервера {ex.Message}");
                canGetData = false;
            }
        }
    }
}