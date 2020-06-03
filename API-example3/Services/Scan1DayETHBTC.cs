using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;

namespace BolliBotIndicatorEndpoints.Services
{
    public class Scan1DayETHBTC : IScan1DayETHBTC
    {
        IAmazonDynamoDB AmazonDynamoDB = new AmazonDynamoDBClient();

        public Scan1DayETHBTC(AmazonDynamoDBClient amazonDynamoDB)
        {
            AmazonDynamoDB = amazonDynamoDB;
        }

        public async Task<Document> scanBTC(long timestamp)
        {
            try
            {
                Table table = Table.LoadTable(AmazonDynamoDB, "BTC1DayIndicatorSets");

                Document doc = await table.GetItemAsync(timestamp.ToString());

                return doc;
            }
            catch
            {
                Console.WriteLine("error connecting to BTC 1 Day indicator sets");
                return null;
            }
        }
        public async Task<Document> scanETH(long timestamp)
        {
            try
            {
                Table table = Table.LoadTable(AmazonDynamoDB, "ETH1DayIndicatorSets");

                Document doc = await table.GetItemAsync(timestamp.ToString());

                return doc;
            }
            catch
            {
                Console.WriteLine("error connecting to ETH 1 Day indicator sets");
                return null;
            }
        }

        public async Task<List<Document>> ETHScanBatch(List<long> timestamps)
        {
            try
            {
                Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH1DayIndicatorSets");
                //Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC5MinOHLC");

                DocumentBatchGet ETHdocumentBatchGet = ETHtable.CreateBatchGet();

                foreach (long timestamp in timestamps)
                {
                    ETHdocumentBatchGet.AddKey(timestamp.ToString());
                }

                await ETHdocumentBatchGet.ExecuteAsync();

                return ETHdocumentBatchGet.Results;
            }
            catch
            {
                Console.WriteLine("Error connecting to ETH 1 day indicators");

                return null;
            }
        }
        public async Task<List<Document>> ETHScanBatchOHLCV(List<long> timestamps)
        {
            try
            {
                //Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinOHLC");
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC1DayOHLC");

                DocumentBatchGet ETHdocumentBatchGet = BTCtable.CreateBatchGet();

                foreach (long timestamp in timestamps)
                {
                    ETHdocumentBatchGet.AddKey(timestamp.ToString());
                }

                await ETHdocumentBatchGet.ExecuteAsync();

                return ETHdocumentBatchGet.Results;
            }
            catch
            {
                Console.WriteLine("Error connecting to BTC 1 day indicators");

                return null;
            }
        }

        public async Task<List<Document>> BTCScanBatch(List<long> timestamps)
        {
            try
            {
                //Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinOHLC");
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC1DayIndicatorSets");

                DocumentBatchGet ETHdocumentBatchGet = BTCtable.CreateBatchGet();

                foreach (long timestamp in timestamps)
                {
                    ETHdocumentBatchGet.AddKey(timestamp.ToString());
                }

                await ETHdocumentBatchGet.ExecuteAsync();

                return ETHdocumentBatchGet.Results;
            }
            catch
            {
                Console.WriteLine("Error connecting to BTC 1 day indicators");

                return null;
            }
        }

        public async Task<List<Document>> BTCScanBatchOHLCV(List<long> timestamps)
        {
            try
            {
                //Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinOHLC");
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC1DayOHLC");

                DocumentBatchGet ETHdocumentBatchGet = BTCtable.CreateBatchGet();

                foreach (long timestamp in timestamps)
                {
                    ETHdocumentBatchGet.AddKey(timestamp.ToString());
                }

                await ETHdocumentBatchGet.ExecuteAsync();

                return ETHdocumentBatchGet.Results;
            }
            catch
            {
                Console.WriteLine("Error connecting to BTC 1 day indicators");

                return null;
            }
        }
    }
}
