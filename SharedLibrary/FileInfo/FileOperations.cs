using NLog;
using SharedLibrary.Helper;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SharedLibrary.FileInfo
{
    public static class FileOperations
    {
        public static string FileName => Path.Combine(HelperMethods.GetAssemblyPath(), FileConstants.FileName);

        public static (SettingsFileInfo Settings, string Message) GetFileInfo(Logger logger = null)
        {
            string fileName = FileName;
            SettingsFileInfo settings = new SettingsFileInfo();

            if (!File.Exists(fileName))
            {
                (bool isSuccess, string message) = SaveFileInfo(settings, logger: logger);
                return (settings, $"Данные не полные, заполните все необходимые данные. {message}");
            }
            else
            {
                using (Stream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    IFormatter formatter = new BinaryFormatter();
                    if (stream.Length != 0)
                    {
                        return ((SettingsFileInfo)formatter.Deserialize(stream), null);
                    }
                    else
                    {
                        return (settings, "Данные не полные, заполните все необходимые данные.");
                    }
                }
            }
        }

        public static async Task<SettingsFileInfo> GetFileInfoAsync(Logger logger = null)
        {
            (SettingsFileInfo settings, string message) = GetFileInfo(logger: logger);

            if (!string.IsNullOrEmpty(message))
            {
                await HelperMethods.Message(message, logger: logger);
            }

            return settings;
        }

        public static (bool IsSuccess, string Message) SaveFileInfo(SettingsFileInfo settings, Logger logger = null)
        {
            try
            {
                if (settings != null && settings.IsValid())
                {
                    using (Stream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, settings);
                        return (true, null);
                    }
                }
                else
                {
                    return (true, "Данные не полные, заполните все необходимые данные.");
                }
            }
            catch (Exception ex)
            {
                return (false, ex.ToString());
            }
        }

        public static async Task<bool> SaveFileInfoAsync(SettingsFileInfo settings, Logger logger = null)
        {
            (bool isSuccess, string message) = SaveFileInfo(settings, logger: logger);
            if (!string.IsNullOrEmpty(message))
            {
                await HelperMethods.Message(message, logger: logger);
            }
            return isSuccess;
        }
    }
}