using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Models;

namespace Common.Adapters
{
    public class PriceFileFormatAdapter : IPriceFileFormatAdapter
    {
        private static readonly DateTime DtGlobalStart = new DateTime(2000, 1, 1);

        public async Task<List<PriceBarForStore>> ReadPriceBars(BinaryReader reader)
        {
            var priceList = new List<PriceBarForStore>();
            while (reader.BaseStream.Length - reader.BaseStream.Position >= sizeof(int) + sizeof(double) + (5 * sizeof(decimal)))
            {
                var priceBar = new PriceBarForStore
                {
                    Volume = reader.ReadInt32(),
                    Time = GetDateTimeFromSeconds(reader.ReadDouble()),
                    BidClose = reader.ReadDecimal(),
                    BidOpen = reader.ReadDecimal(),
                    BidHigh = reader.ReadDecimal(),
                    BidLow = reader.ReadDecimal(),
                    Spread = reader.ReadDecimal(),
                };
                if (priceBar.BidOpen == 0.0m)
                {
                    break;
                }

                priceList.Add(priceBar);
            }

            return priceList;
        }

        public async Task<InstrumentForStore> ReadInstrument(BinaryReader reader)
        {
            var instrument = new InstrumentForStore
            {
                Digits = reader.ReadInt32(),
                FromTime = GetDateTimeFromSeconds(reader.ReadDouble()),
                ToTime = GetDateTimeFromSeconds(reader.ReadDouble()),
                PointSize = reader.ReadDecimal(),
                Instrument = reader.ReadString(),
                OfferId = reader.ReadString(),
                TimeFrame = reader.ReadString(),
            };
            return instrument;
        }

        public async Task<int> Write(InstrumentForStoreBase data, BinaryWriter bw)
        {
            bw.Write(data.Digits);
            bw.Write(GetSeconds(data.FromTime));
            bw.Write(GetSeconds(data.ToTime));
            bw.Write(data.PointSize);
            bw.Write(data.Instrument);
            bw.Write(data.OfferId);
            bw.Write(data.TimeFrame);

            return 1;
        }

        public async Task<int> Write(PriceBarForStore data, BinaryWriter bw)
        {
            bw.Write(data.Volume);
            bw.Write(GetSeconds(data.Time));
            bw.Write(data.BidClose);
            bw.Write(data.BidOpen);
            bw.Write(data.BidHigh);
            bw.Write(data.BidLow);
            bw.Write(data.Spread);

            return 1;
        }

        private DateTime GetDateTimeFromSeconds(double seconds)
        {
            var result = DtGlobalStart.AddSeconds(seconds);
            return result;
        }

        private double GetSeconds(DateTime date)
        {
            var seconds = (date - DtGlobalStart).TotalSeconds;
            return seconds;
        }
    }
}
