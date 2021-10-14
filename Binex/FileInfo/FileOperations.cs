using NLog;
using SharedLibrary.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Binex.FileInfo
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
                    return settings;
                }
                else
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    return (SettingsFileInfo)formatter.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.Message, logger: logger);
                return settings;
            }
        }

        public static async Task<bool> SaveFileInfo(SettingsFileInfo settings, Logger logger = null)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                formatter.Serialize(stream, settings);
                stream.Close();
                return true;
            }
            catch (Exception ex)
            {
                await HelperMethods.Message(ex.Message, logger: logger);
                return false;
            }
        }
    }
}