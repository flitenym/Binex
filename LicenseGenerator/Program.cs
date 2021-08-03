using System;

namespace LicenseGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            bool IsEnd = false;

            while (!IsEnd) {
                var result = GetLicenseData();
                if (result.IsSuccess)
                {
                    License license = new License();
                    license.CreateLicense(result.StartDate.Value, result.Months.Value, "Binex", result.Name, "asd");
                    IsEnd = true;
                }
                else
                {
                    continue;
                }
            } 
        }

        static (bool IsSuccess, string Name, DateTime? StartDate, int? Months) GetLicenseData()
        {
            Console.WriteLine("Введите имя пользователя, пример: {Николай}");
            var name = Console.ReadLine();
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Имя пустое, задайте имя");
                return default;
            }

            Console.WriteLine("Введите дату начала лицензии, пример: {31.12.2000}");
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

            return (true, name, date, months);
        }
    }
}
