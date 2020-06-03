using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using System.Threading;

namespace BolliBotIndicatorEndpoints.Services
{
    public class APIKeys : IAPIKeys
    {
        IAmazonDynamoDB AmazonDynamoDB = new AmazonDynamoDBClient();

        public APIKeys(AmazonDynamoDBClient amazonDynamoDB)
        {
            this.AmazonDynamoDB = amazonDynamoDB;
        }

        public async Task<Document> getAPIKey(string key)
        {
            try
            {
                Table APIKeyTable = Table.LoadTable(AmazonDynamoDB, "APIKeys");

                return await APIKeyTable.GetItemAsync(key);
            }
            catch
            {
                return null;
            }
        }
    }
}
