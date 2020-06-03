using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BitExExternal.Models
{
    public class OrderPost
    {
        [JsonProperty]
        public string OrderType { get; set; }
        [JsonProperty]
        public double? OrderQuantity { get; set; }
        [JsonProperty]
        public string Contract { get; set; }
        [JsonProperty]
        public string Price { get; set; }
        [JsonProperty]
        public string Side { get; set; }
    }
}
