using System.Collections.Generic;
using System.Linq;
using Common.Models;

namespace ExpertCollection
{
    public static class Extensions
    {
        public static int IndexOfMaximumElement(this IEnumerable<PriceBarForStore> bars)
        {
            var list = bars.ToArray();
            var size = list.Length;

            if (size < 2)
                return 0;

            var maxValue = list[0].BidHigh;
            var maxIndex = 0;

            for (var i = 1; i < size; i+=1)
            {
                var thisValue = list[i].BidHigh;
                if (thisValue > maxValue)
                {
                    maxValue = thisValue;
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        public static int IndexOfMinimumElement(this IEnumerable<PriceBarForStore> bars)
        {
            var list = bars.ToArray();
            var size = list.Length;

            if (size < 2)
                return 0;

            var minValue = list[0].BidLow;
            var minIndex = 0;

            for (var i = 1; i < size; i+=1)
            {
                var thisValue = list[i].BidLow;
                if (thisValue < minValue)
                {
                    minValue = thisValue;
                    minIndex = i;
                }
            }

            return minIndex;
        }
    }
}
