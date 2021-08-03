using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace LicenseGenerator
{
    public class License
    {
        public string LicenseTermsData { get; set; }
        public string Signature { get; set; }

        /// use a private key to generate a secure license file. the private key must match the public key accessible to
        /// the system validating the license.
        /// </summary>
        /// <param name="start">applicable start date for the license file.</param>
        /// <param name="end">applicable end date for the license file</param>
        /// <param name="productName">applicable product name</param>
        /// <param name="userName">user-name</param>
        /// <param name="privateKey">the private key (in XML form)</param>
        /// <returns>secure, public license, validated with the public part of the key</returns>
        public void CreateLicense(DateTime start, int month, string productName, string userName, string privateKey)
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

            // setup the dsa from the private key:
            dsa.FromXmlString(privateKey);

            // get the byte-array of the licence terms:
            byte[] license = terms.GetLicenseData();

            // get the signature:
            byte[] signature = dsa.SignData(license);

            // now create the license object:  
            LicenseTermsData = Convert.ToBase64String(license);
            Signature = Convert.ToBase64String(signature);
        }

        /// validate license file and return the license terms.
        /// </summary>
        /// <param name="license"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        internal LicenseTerms GetValidTerms(License license, string publicKey)
        {
            // create the crypto-service provider:
            DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();

            // setup the provider from the public key:
            dsa.FromXmlString(publicKey);

            // get the license terms data:
            byte[] terms = Convert.FromBase64String(license.LicenseTermsData);

            // get the signature data:
            byte[] signature = Convert.FromBase64String(license.Signature);

            // verify that the license-terms match the signature data
            if (dsa.VerifyData(terms, signature))
                return LicenseTerms.FromString(license.LicenseTermsData);
            else
                throw new SecurityException("Signature Not Verified!");
        }

        public bool IsValidLicense(License license, string publicKey)
        {
            var terms = GetValidTerms(license, publicKey);
            if (terms.Month == 0)
            {
                return true;
            }
            else if (terms.StartDate.AddMonths(terms.Month).Date <= DateTime.Now.Date)
            {
                return true;
            }

            return false;
        }
    }
}