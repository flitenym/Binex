using System;
using System.IO;
using System.Reflection;

namespace LicenseGenerator
{
    class Program
    {
        public const string PrivateKeyName = "LicenseGenerator.PrivateKey.xml";

        static void Main(string[] args)
        {
            bool IsEnd = false;

            while (!IsEnd)
            {
                var result = GetLicenseData();
                if (result.IsSuccess)
                {
                    var privateKeyBytes = GetContentFromAssembly(PrivateKeyName);
                    License license = new License();
                    license.CreateLicense(result.StartDate.Value, result.Months.Value, result.ProductName, result.Name, privateKeyBytes);
                    IsEnd = true;
                }
                else
                {
                    continue;
                }
            }
        }

        public static byte[] GetContentFromAssembly(string filePath)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filePath);
            if (stream != null)
            {
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                return ba;
            }

            return null;
        }

        static (bool IsSuccess, string Name, string ProductName, DateTime? StartDate, int? Months) GetLicenseData()
        {
            Console.WriteLine("Введите имя пользователя, пример: {Николай}");
            var name = Console.ReadLine();
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Имя пустое, задайте имя");
                return default;
            }

            Console.WriteLine("Введите название ПО");
            var productName = Console.ReadLine();
            if (string.IsNullOrEmpty(productName))
            {
                Console.WriteLine("Название пустое, задайте название ПО");
                return default;
            }

            Console.WriteLine("Введите дату начала лицензии, пример: {31.12.2020}");
            var dateValue = Console.ReadLine();
            if (!DateTime.TryParse(dateValue, out var date))
            {
                Console.WriteLine("Дата задана неверно");
                return default;
            }

            Console.WriteLine("Введите количество месяцев для лицензии или 0 если бессрочная, пример: {1}");
            var monthsValue = Console.ReadLine();
            if (!int.TryParse(monthsValue, out var months))
            {
                Console.WriteLine("Количество месяцев введено не верно");
                return default;
            }

            return (true, name, productName, date, months);
        }
    }
}
