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
        public static async Task<SettingsFileInfo> GetFileInfo(Logger logger = null)
        {
            string fileName = FileName;
            SettingsFileInfo settings = new SettingsFileInfo();

            try
            {
                if (!File.Exists(fileName))
                {
                    await SaveFileInfo(settings);
                    await HelperMethods.Message("Данные не полные, заполните все необходимые данные.", logger: logger);
                    return settings;
                }
                else
                {
                    using (Stream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        if (stream.Length != 0)
                        {
                            return (SettingsFileInfo)formatter.Deserialize(stream);
                        }
                        else
                        {
                            await HelperMethods.Message("Данные не полные, заполните все необходимые данные.", logger: logger);
                            return settings;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.Message, logger: logger);
                await HelperMethods.Message("Данные не полные, заполните все необходимые данные.", logger: logger);
                return settings;
            }
        }

        public static async Task<bool> SaveFileInfo(SettingsFileInfo settings, Logger logger = null)
        {
            try
            {
                if (settings != null && settings.IsValid())
                {
                    using (Stream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, settings);
                        return true;
                    }
                }
                else
                {
                    await HelperMethods.Message("Данные не полные, заполните все необходимые данные.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.Message, logger: logger);
                return false;
            }
        }
    }
}