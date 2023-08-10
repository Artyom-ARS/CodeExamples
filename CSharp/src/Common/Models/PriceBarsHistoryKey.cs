namespace Common.Models
{
    public class PriceBarsHistoryKey
    {
        public string Instrument { get; set; }

        public TimeFrame TimeFrame { get; set; }

        public int BarsCount { get; set; }
    }
}
