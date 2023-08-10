namespace Common.Models
{
    public class GgV3Parameters : GgV1Parameters
    {
        public int StopLoss { get; set; }

        public int MaxRisk { get; set; }

        public int FreezeAfterStopLossMinutes { get; set; }
    }
}
