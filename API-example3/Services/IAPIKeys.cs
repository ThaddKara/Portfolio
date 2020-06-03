using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;

namespace BolliBotIndicatorEndpoints.Services
{
    interface IAPIKeys
    {
        Task<Document> getAPIKey(string key);
    }
}
