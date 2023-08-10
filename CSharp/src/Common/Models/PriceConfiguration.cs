namespace Common.Models
{
    public class PriceConfiguration
    {
        public string Instrument { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public string TimeFrame { get; set; }

        public bool? Disabled { get; set; }

        public int PastDays { get; set; }

        public int Year { get; set; }
    }
}
