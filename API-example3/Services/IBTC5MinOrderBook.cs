using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BolliBotIndicatorEndpoints.Services
{
    interface IBTC5MinOderBook
    {
        Task putJson(string key, string bitmexjson, string deribitjson);
        Task<string> getJson(string key, string exchange);
    }
}
