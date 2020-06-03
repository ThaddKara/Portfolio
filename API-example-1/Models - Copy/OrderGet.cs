using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace BitExExternal.Models
{
    public class OrderGet
    {
        [JsonProperty]
        public string Id { get; set; }
        [JsonProperty]
        public string Contract { get; set; }
        [JsonProperty]
        public double OrderQuantity { get; set; }
        [JsonProperty]
        public double Price { get; set; }
        [JsonProperty]
        public string OrderType { get; set; }
    }
}
