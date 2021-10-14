using System;

namespace Binex.FileInfo
{
    [Serializable]
    public class SettingsFileInfo
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string Emails { get; set; }
        public string EmailLogin { get; set; }
        public string EmailPassword { get; set; }
        public string CronExpression { get; set; }
        public string IsCurrenciesSell { get; set; }
        public string IsDustSell { get; set; }
        public string IsTransferFromFuturesToSpot { get; set; }
    }
}
