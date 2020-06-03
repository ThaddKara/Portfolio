using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BolliBotIndicatorEndpoints.Models
{
    public class SummaryModel
    {
        public SummaryModel(long Timestamp, float Ma200, float Ma150, float Ma100, float Ma50,
            float Ma20, float Ema200, float Ema150, float Ema100, float Ema50, float Ema20, 
            float AvgGain, float AvgLoss, float Rsi, float Upperband, float Middleband,
            float Lowerband, float Percentb)
        {
            timestamp = Timestamp;
            ma200 = Ma200;
            ma150 = Ma150;
            ma100 = Ma100;
            ma50 = Ma50;
            ma20 = Ma20;
            ema200 = Ema200;
            ema150 = Ema150;
            ema100 = Ema100;
            ema50 = Ema50;
            ema20 = Ema20;
            BB = new BBcompenents(Percentb, Upperband, Lowerband, Middleband);
            rsi = new RSI(AvgGain, AvgLoss, Rsi);
        }

        public long timestamp { get; set; }
        public float ma200 { get; set; }
        public float ma150 { get; set; }

        public float ma100 { get; set; }
        public float ma50 { get; set; }
        public float ma20 { get; set; }
        public float ema200 { get; set; }
        public float ema150 { get; set; }
        public float ema100 { get; set; }
        public float ema50 { get; set; }
        public float ema20 { get; set; }
        public BBcompenents BB { get; set; }
        public RSI rsi { get; set; }
    }

    public class RSI
    {
        public RSI(float AvgGain, float AvgLoss, float Rsi)
        {
            avgGain = AvgGain;
            avgLoss = AvgLoss;
            rsi = Rsi;
        }


        public float avgGain { get; set; }
        public float avgLoss { get; set; }
        public float rsi { get; set; }
    }

    public class BBcompenents
    {
        public BBcompenents(float Percentb, float Upperband, float Lowerband, float Middleband)
        {
            percentb = Percentb;
            upperband = Upperband;
            lowerband = Lowerband;
            middleband = Middleband;
        }

        public float percentb { get; set; }
        public float upperband { get; set; }
        public float lowerband { get; set; }
        public float middleband { get; set; }
    }
}
