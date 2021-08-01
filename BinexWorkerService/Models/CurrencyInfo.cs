namespace BinexWorkerService.Models
{
    public class CurrencyInfo
    {
        public string Asset { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsSuccessSell { get; set; }
        public bool IsWasNeedToBTC { get; set; }

        public string GetStringFromBool(bool boolValue)
        {
            return boolValue ? "Да" : "Нет";
        }
        public string GetElementHTML()
        {
            string color = IsSuccessSell ? "4FC3A1" : "FF6161";
            return $@"
<tr>
	<td style=""text-align: center; padding: 8px; border-right: 1px solid #f8f8f8; font-size: 12px; background: #{color};"" align=""center"">{Asset}</td>
	<td style=""text-align: center; padding: 8px; border-right: 1px solid #f8f8f8; font-size: 12px;"" align=""center"">{GetStringFromBool(IsSuccessSell)}</td>
	<td style=""text-align: center; padding: 8px; border-right: 1px solid #f8f8f8; font-size: 12px;"" align=""center"">{GetStringFromBool(IsWasNeedToBTC)}</td>
</tr>
";
        }
    }
}