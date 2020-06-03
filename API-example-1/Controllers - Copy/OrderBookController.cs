using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using BitExExternal.Models;
using System.Text;
using Newtonsoft.Json;

namespace BitExExternal.Controllers
{
    [Route("api/v2")]
    [ApiController]
    public class OrderBookController : ControllerBase
    {
        /// <summary>
        /// get orderbook containing all 
        /// </summary>
        /// <param name="Authorization"></param>
        /// <param name="book"></param>
        /// <returns></returns>
        [Route("orderbooks")]
        [HttpGet]
        public async Task<IActionResult> getbook([FromHeader]string Authorization, [FromBody]OrderBook book)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Table UserTable = Table.LoadTable(amazonDynamoDBClient, "UserDetailsRegistry");
                Document searchdetails = new Document();

                try
                {
                    string[] autharray = Authorization.Split(' ');
                    string authtype = autharray[0];
                    string authstring = autharray[1];

                    if (authtype == "Basic")
                    {
                        byte[] data = Convert.FromBase64String(authstring);
                        string decodedString = Encoding.UTF8.GetString(data);

                        string[] splitauth = decodedString.Split(":");
                        string id = splitauth[0];
                        string secret = splitauth[1]; Console.WriteLine(id + secret);

                        ScanOperationConfig scanOperation = new ScanOperationConfig();
                        ScanFilter scanFilter = new ScanFilter();
                        scanFilter.AddCondition("DefaultId", ScanOperator.Equal, id);
                        Search search = UserTable.Scan(scanOperation);
                        List<Document> details = await search.GetRemainingAsync();

                        foreach (Document doc in details)
                        {
                            if (!string.IsNullOrWhiteSpace(doc["DefaultId"]))
                            {
                                if (doc["DefaultSecret"] == secret && doc["DefaultId"] == id)
                                {
                                    Console.WriteLine("successful auth");
                                    searchdetails = doc;
                                    break;
                                }
                            }
                            else
                            {
                                continue;
                            }

                            return BadRequest("Id not found");
                        }
                    }
                    else
                    {
                        return BadRequest("Bad authorization");
                    }
                }
                catch
                {
                    return BadRequest("authorizaion failed");
                }

                Table orderbooktable;
                if (book.symbol == "SPYUSD") { orderbooktable = Table.LoadTable(amazonDynamoDBClient, "OrderBook"); }
                else { return BadRequest("bad symbol"); }
                ScanOperationConfig config = new ScanOperationConfig();
                ScanFilter filter = new ScanFilter();
                filter.AddCondition("Price", ScanOperator.IsNotNull);
                config.Filter = filter;
                config.AttributesToGet.Add("Price");
                config.AttributesToGet.Add("Volume");
                config.AttributesToGet.Add("Type");

                Search searchs = orderbooktable.Scan(config);
                List<Document> orderbook = await searchs.GetRemainingAsync();
                List<ReturnOrderBook> rob = new List<ReturnOrderBook>();

                foreach (Document d in orderbook)
                {
                    rob.Add(new ReturnOrderBook() { Price = d["Price"].ToString(), Volume = d["Volume"].ToString(), Type = d["Type"].ToString() });
                }

                string json = JsonConvert.SerializeObject(rob);
                return Content(json, "application/json");
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }
    }
}