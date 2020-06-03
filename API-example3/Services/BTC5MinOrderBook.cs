using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System.Threading.Tasks;

namespace BolliBotIndicatorEndpoints.Services
{
    class BTC5MinOrderBook : IBTC5MinOderBook
    {
        IAmazonDynamoDB amazonDynamoDB = new AmazonDynamoDBClient();

        public BTC5MinOrderBook(IAmazonDynamoDB amazonDynamoDB)
        {
            this.amazonDynamoDB = amazonDynamoDB;
        }

        public async Task putJson(string key, string bitmexjson, string deribitjson)
        {
            try
            {
                Table table = Table.LoadTable(amazonDynamoDB, "BTC5MinOrderBook");
                Document doc = new Document();
                doc["Timestamp"] = key;
                doc["Bitmexbook"] = bitmexjson;
                doc["Deribitbook"] = deribitjson;
                await table.PutItemAsync(doc);
            }
            catch
            {
                Console.WriteLine("error putJson");
            }
        }

        public async Task<string> getJson(string key, string exchange)
        {
            try
            {
                Table table = Table.LoadTable(amazonDynamoDB, "BTC5MinOrderBook");
                Document doc = await table.GetItemAsync(key);
                
                if (exchange=="bitmex")
                {
                    return doc["Bitmexbook"];
                }
                else if (exchange=="deribit")
                {
                    return doc["Deribitbook"];
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                Console.WriteLine("error getJson");
                return null;
            }
        }
    }
}
