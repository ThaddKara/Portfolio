using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BolliBotIndicatorEndpoints.Models
{
    public class OHLCVModel
    {
        public OHLCVModel(double open, double high, double low, double close, double volume)
        {
            this.open = open;
            this.high = high;
            this.low = low;
            this.close = close;
            this.volume = volume;
        }

        public double open { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double close { get; set; }
        public double volume { get; set; }
    }
}
