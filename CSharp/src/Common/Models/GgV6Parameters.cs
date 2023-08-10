namespace Common.Models
{
    public class GgV6Parameters : GgV4Parameters
    {
        public string ZZHighTimeFrame { get; set; }

        public int ZZHighBeforeBarsCount { get; set; }

        public int ZZHighAfterBarsCount { get; set; }

        public int ZZHighBarsCount { get; set; }

        public string ZZLowTimeFrame { get; set; }

        public int ZZLowBeforeBarsCount { get; set; }

        public int ZZLowAfterBarsCount { get; set; }

        public int ZZLowBarsCount { get; set; }

        public int CciPeriod { get; set; }

        public decimal CciThreshold { get; set; }
    }
}
