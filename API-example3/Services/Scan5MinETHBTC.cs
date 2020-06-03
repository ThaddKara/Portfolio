using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BolliBotIndicatorEndpoints.Services
{
    class Scan5MinETHBTC : IScan5MinETHBTC
    {
        IAmazonDynamoDB AmazonDynamoDB = new AmazonDynamoDBClient();

        public Scan5MinETHBTC(IAmazonDynamoDB amazonDynamoDBClient)
        {
            AmazonDynamoDB = amazonDynamoDBClient;
        }

        public async Task<Document> ETHScan(long timestamp)
        {
            try
            {
                Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinIndicatorSets");

                Document doc = await ETHtable.GetItemAsync(timestamp.ToString());

                return doc;
            }
            catch
            {
                Console.WriteLine("error conneting to 5 min ETH indicator sets");
                return null;
            }
        }

        public async Task<Document> BTCScan(long timestamp)
        {
            try
            {
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC5MinIndicatorSets");

                Document doc = await BTCtable.GetItemAsync(timestamp.ToString());

                return doc;
            }
            catch
            {
                Console.WriteLine("error connecting to 5 min BTC indicator sets");
                return null;
            }
        }

        public async Task<List<Document>> ETHScanBatch(List<long> timestamps)
        {
            try
            {
                Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinIndicatorSets");
                //Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC5MinOHLC");

                DocumentBatchGet ETHdocumentBatchGet = ETHtable.CreateBatchGet();

                foreach(long timestamp in timestamps)
                {
                    ETHdocumentBatchGet.AddKey(timestamp.ToString());
                }

                await ETHdocumentBatchGet.ExecuteAsync();

                return ETHdocumentBatchGet.Results;
            }
            catch
            {
                Console.WriteLine("Error connecting to ETH 5Min OHLC");

                return null;
            }
        }
        public async Task<List<Document>> ETHScanBatchOHLCV(List<long> timestamps)
        {
            try
            {
                //Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinOHLC");
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinOHLC");

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
                Console.WriteLine("Error connecting to BTC 1 hour indicators");

                return null;
            }
        }
        public async Task<List<Document>> BTCScanBatch(List<long> timestamps)
        {
            try
            {
                //Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinOHLC");
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC5MinIndicatorSets");

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
                Console.WriteLine("Error connecting to BTC 5Min OHLC");

                return null;
            }
        }
        public async Task<List<Document>> BTCScanBatchOHLCV(List<long> timestamps)
        {
            try
            {
                //Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinOHLC");
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC5MinOHLC");

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
                Console.WriteLine("Error connecting to BTC 1 hour indicators");

                return null;
            }
        }
    }
}
