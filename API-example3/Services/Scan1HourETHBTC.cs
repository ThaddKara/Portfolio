using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BolliBotIndicatorEndpoints.Services
{
    class Scan1HourETHBTC : IScan1HourETHBTC
    {
        IAmazonDynamoDB AmazonDynamoDB = new AmazonDynamoDBClient();

        public Scan1HourETHBTC(IAmazonDynamoDB amazonDynamoDB)
        {
            AmazonDynamoDB = amazonDynamoDB;
        }

        public async Task<Document> getByTimestampETH(long timestamp)
        {
            try
            {
                Table table = Table.LoadTable(AmazonDynamoDB, "ETH1HourIndicatorSets");
                Document doc =  await table.GetItemAsync(timestamp.ToString());
                return doc;
            }
            catch
            {
                Console.WriteLine("error connecting to 1 hour ETH indicator sets");
                return null;
            }
        }

        

        public async Task<Document> getByTimestampBTC(long timestamp)
        {
            try
            {
                Table table = Table.LoadTable(AmazonDynamoDB, "BTC1HourIndicatorSets");
                Document doc = await table.GetItemAsync(timestamp.ToString());
                return doc;
            }
            catch
            {
                Console.WriteLine("error connecting to 1 hour BTC indicator sets");
                return null;
            }
        }

        public async Task<List<Document>> ETHScanBatch(List<long> timestamps)
        {
            try
            {
                Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH1HourIndicatorSets");
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
                Console.WriteLine("Error connecting to ETH 1 hour indicators");

                return null;
            }
        }
        public async Task<List<Document>> ETHScanBatchOHLCV(List<long> timestamps)
        {
            try
            {
                //Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinOHLC");
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "1HourOHLC");

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
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC1HourIndicatorSets");

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

        public async Task<List<Document>> BTCScanBatchOHLCV(List<long> timestamps)
        {
            try
            {
                //Table ETHtable = Table.LoadTable(AmazonDynamoDB, "ETH5MinOHLC");
                Table BTCtable = Table.LoadTable(AmazonDynamoDB, "BTC1HourOHLC");

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
