using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace BolliBotIndicatorEndpoints.Services
{
    public class GenericGet : IGenericGet
    {
        IAmazonDynamoDB AmazonDynamoDBClient = new AmazonDynamoDBClient();
        
        public GenericGet(AmazonDynamoDBClient amazonDynamoDBClient)
        {
            AmazonDynamoDBClient = amazonDynamoDBClient;
        }

        public async Task<Document> tableGet(string tableName, string key)
        {
            try
            {
                Table table = Table.LoadTable(AmazonDynamoDBClient, tableName);

                return await table.GetItemAsync(key);
            }
            catch
            {
                Console.WriteLine("error generic get");
                return null;
            }
        }

        public async Task tablePut(string tableName, Document doc)
        {
            try
            {
                Table table = Table.LoadTable(AmazonDynamoDBClient, tableName);

                await table.PutItemAsync(doc);
            }
            catch
            {
                Console.WriteLine("error generic put");
            }
        }

        public async Task updatePut(string tableName, Document doc)
        {
            try
            {
                Table table = Table.LoadTable(AmazonDynamoDBClient, tableName);

                await table.UpdateItemAsync(doc);
            }
            catch
            {
                Console.WriteLine("error generic update");
            }
        }

        public async Task deletePut(string tableName, Document doc)
        {
            try
            {
                Table table = Table.LoadTable(AmazonDynamoDBClient, tableName);

                await table.DeleteItemAsync(doc);
            }
            catch
            {
                Console.WriteLine("error generic delete");
            }
        }
    }
}
