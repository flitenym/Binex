using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace LicenseGenerator
{
    public class License
    {
        public string LicenseTermsData { get; set; }
        public string Signature { get; set; }
        public string PublicKey { get; set; }

        /// use a private key to generate a secure license file. the private key must match the public key accessible to
        /// the system validating the license.
        /// </summary>
        /// <param name="start">applicable start date for the license file.</param>
        /// <param name="end">applicable end date for the license file</param>
        /// <param name="productName">applicable product name</param>
        /// <param name="userName">user-name</param>
        /// <param name="privateKey">the private key (in XML form)</param>
        /// <returns>secure, public license, validated with the public part of the key</returns>
        public void CreateLicense(DateTime start, int month, string productName, string userName, byte[] privateKeyBytes)
        {
            // create the licence terms:
            LicenseTerms terms = new LicenseTerms()
            {
                StartDate = start,
                Month = month,
                ProductName = productName,
                UserName = userName
            };

            // create the crypto-service provider:
            DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();

            dsa.ImportPkcs8PrivateKey(privateKeyBytes, out int len);

            // get the byte-array of the licence terms:
            byte[] license = terms.GetLicenseData();

            // get the signature:
            byte[] signature = dsa.SignData(license);

            // now create the license object:
            LicenseTermsData = Convert.ToBase64String(license);
            Signature = Convert.ToBase64String(signature);
            PublicKey = dsa.ToXmlString(false);

            XDocument xdoc = new XDocument();
            var licenseXmlElement = new XElement(nameof(License));
            licenseXmlElement.Add(new XElement(nameof(LicenseTermsData), LicenseTermsData));
            licenseXmlElement.Add(new XElement(nameof(Signature), Signature));
            licenseXmlElement.Add(new XElement(nameof(PublicKey), PublicKey));
            xdoc.Add(licenseXmlElement);
            xdoc.Save("License.lic");
        }

        /// validate license file and return the license terms.
        /// </summary>
        /// <param name="license"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        internal static (LicenseTerms LicenseTerms, string Error) GetValidTerms(XDocument xdoc)
        {
            try
            {
                var licenseXmlElement = xdoc.Element(nameof(License));

                License license = new License()
                {
                    LicenseTermsData = licenseXmlElement.Element(nameof(LicenseTermsData))?.Value,
                    Signature = licenseXmlElement.Element(nameof(Signature))?.Value,
                    PublicKey = licenseXmlElement.Element(nameof(PublicKey))?.Value,
                };

                // create the crypto-service provider:
                DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();

                // setup the provider from the public key:
                dsa.FromXmlString(license.PublicKey);

                // get the license terms data:
                byte[] terms = Convert.FromBase64String(license.LicenseTermsData);

                // get the signature data:
                byte[] signature = Convert.FromBase64String(license.Signature);

                // verify that the license-terms match the signature data
                if (dsa.VerifyData(terms, signature))
                    return (LicenseTerms.FromString(license.LicenseTermsData), null);
                else
                    return (null, "Поврежденный файл лицензии");
            }
            catch (Exception ex)
            {
                return (null, $"Ошибка чтения файла лицензии: {ex.Message}");
            }
        }

        public static (bool IsValid, DateTime StartDate, string EndDate, string UserName, string ProductName, string Message) IsValidLicense(XDocument xdoc)
        {
            (LicenseTerms terms, string error) = GetValidTerms(xdoc);

            if (terms == null)
            {
                return (false, default, default, default, default, error);
            }

            if (terms.Month == 0)
            {
                return (true, terms.StartDate, "Бессрочно", terms.UserName, terms.ProductName, null);
            }
            else if (terms.StartDate.AddMonths(terms.Month).Date >= DateTime.Now.Date)
            {
                var days = (DateTime.Now.Date - terms.StartDate.AddMonths(terms.Month).Date).TotalDays;
                if (days <= 3)
                {
                    return (true, terms.StartDate, terms.StartDate.AddMonths(terms.Month).ToShortDateString(), terms.UserName, terms.ProductName, $"Лицензия истекает через {days} {(days == 1 ? "день" : "дня")}");
                }
                return (true, terms.StartDate, terms.StartDate.AddMonths(terms.Month).ToShortDateString(), terms.UserName, terms.ProductName, null);
            }
            else
            {
                return (false, default, default, default, default, $"Лицензия истекла, действие лицензии было до {terms.StartDate.AddMonths(terms.Month).Date}");
            }
        }
    }
}