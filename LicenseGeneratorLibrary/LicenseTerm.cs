using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LicenseGenerator
{
    /// terms of the license agreement: it&#39;s not encrypted (but is obscured)
    /// </summary>
    [Serializable]
    internal class LicenseTerms
    {
        /// <summary>
        /// start date of the license agreement.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// registered user name for the license agreement.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// the assembly name of the product that is licensed.
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// the last date on which the software can be used on this license.
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// returns the license terms as an obscure (not human readable) string.
        /// </summary>
        /// <returns></returns>
        public string GetLicenseString()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // create a binary formatter:
                BinaryFormatter bnfmt = new BinaryFormatter();

                // serialize the data to the memory-steam;
                bnfmt.Serialize(ms, this);

                // return a base64 string representation of the binary data:
                return Convert.ToBase64String(ms.GetBuffer());

            }
        }

        /// <summary>
        /// returns a binary representation of the license terms.
        /// </summary>
        /// <returns></returns>
        public byte[] GetLicenseData()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // create a binary formatter:
                BinaryFormatter bnfmt = new BinaryFormatter();

                // serialize the data to the memory-steam;
                bnfmt.Serialize(ms, this);

                // return a base64 string representation of the binary data:
                return ms.GetBuffer();

            }
        }

        /// <summary>
        /// create a new license-terms object from a string-representation of the binary
        /// serialization of the licence-terms.
        /// </summary>
        /// <param name="licenseTerms"></param>
        /// <returns></returns>
        internal static LicenseTerms FromString(string licenseTerms)
        {
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(licenseTerms)))
            {
                // create a binary formatter:
                BinaryFormatter bnfmt = new BinaryFormatter();

                // serialize the data to the memory-steam;
                object value = bnfmt.Deserialize(ms);

                if (value is LicenseTerms)
                    return (LicenseTerms)value;
                else
                    throw new ApplicationException("Invalid Type!");
            }
        }
    }
}