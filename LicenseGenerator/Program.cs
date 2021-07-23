using System;
using System.Security.Cryptography;
using System.Xml;

namespace LicenseGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            bool IsEnd = false;

            while (!IsEnd) {
                var result = License();
                if (result.IsSuccess)
                {
                    MD5 md5 = new MD5CryptoServiceProvider();
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(result.Name + result.Date.Value.ToShortDateString() + 
                        "B37F00E131967D5888350F2307D26FEA9EA86A451C816C3C1E221DD476279EB3D5CC57B28C85AE8662B379C30545F84CBD4262D6DFB7653B8939D6D28D14D3C4");
                    byte[] hash = md5.ComputeHash(data);

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(@"<license>
<Name></Name>
<Date></Date>
<UpdateTo></UpdateTo>
<Signature></Signature>
</license>");
                    doc.ChildNodes[0].SelectSingleNode(@"/license/Name", null).InnerText = result.Name;
                    doc.ChildNodes[0].SelectSingleNode(@"/license/Date", null).InnerText = result.Date.Value.ToShortDateString();
                    doc.ChildNodes[0].SelectSingleNode(@"/license/Signature", null).InnerText = Convert.ToBase64String(hash);

                    doc.Save(System.IO.Path.Combine(Environment.CurrentDirectory, "license.xml"));
                    IsEnd = true;
                }
                else
                {
                    continue;
                }
            } 
        }

        static (bool IsSuccess, string Name, DateTime? Date) License()
        {
            Console.WriteLine("Введите имя пользователя, пример: {Николай}");
            var name = Console.ReadLine();
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Имя пустое, задайте имя");
                return (false, null, null);
            }

            Console.WriteLine("Введите дату, пример: {31.12.2000}");
            var dateValue = Console.ReadLine();
            if (!DateTime.TryParse(dateValue, out var date))
            {
                Console.WriteLine("Дата задана неверно");
                return (false, null, null);
            }

            return (true, name, date);
        }
    }
}
