using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BitExExternal.Models
{
    public class OrderDelete
    {
        [JsonProperty]
        public string OrderId { get; set; }
    }
}
