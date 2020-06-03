using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BolliBotIndicatorEndpoints.Models
{
    public class SummaryModel1Day
    {
        public SummaryModel1Day(long Timestamp, float Ma50,
            float Ma20, float Ema50, float Ema20, 
            float AvgGain, float AvgLoss, float Rsi, float Upperband, float Middleband,
            float Lowerband, float Percentb)
        {
            timestamp = Timestamp;
            //ma200 = Ma200;
            //ma150 = Ma150;
            //ma100 = Ma100;
            ma50 = Ma50;
            ma20 = Ma20;
            //ema200 = Ema200;
            //ema150 = Ema150;
            //ema100 = Ema100;
            ema50 = Ema50;
            ema20 = Ema20;
            BB = new BBcompenents(Percentb, Upperband, Lowerband, Middleband);
            rsi = new RSI(AvgGain, AvgLoss, Rsi);
        }

        public long timestamp { get; set; }
        //public float ma200 { get; set; }
        //public float ma150 { get; set; }
        //public float ma100 { get; set; }
        public float ma50 { get; set; }
        public float ma20 { get; set; }
        //public float ema200 { get; set; }
        //public float ema150 { get; set; }
        //public float ema100 { get; set; }
        public float ema50 { get; set; }
        public float ema20 { get; set; }
        public BBcompenents BB { get; set; }
        public RSI rsi { get; set; }
    }    
}
