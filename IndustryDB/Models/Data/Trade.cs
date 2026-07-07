namespace IndustryDB.Models.Data
{
    public class Trade
    {
        public int TradeId { get; set; }
        public short Year { get; set; }
        public string Region1 { get; set; } = "";
        public string Region2 { get; set; } = "";
        public string Industry1 { get; set; } = "";
        public string Industry2 { get; set; } = "";
        public decimal Amount { get; set; }
        public string FlowType { get; set; } = "";
        public string Country { get; set; } = "";
    }
}
