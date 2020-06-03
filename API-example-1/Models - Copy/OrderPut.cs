using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BitExExternal.Models
{
    public class OrderPut
    {
        [JsonProperty]
        public string OrderId { get; set; }
        [JsonProperty]
        public double? NewPrice { get; set; }
        [JsonProperty]
        public double? NewQuantity { get; set; }
    }
}
