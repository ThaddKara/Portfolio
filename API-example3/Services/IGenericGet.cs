using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;

namespace BolliBotIndicatorEndpoints.Services
{
    interface IGenericGet
    {
        Task<Document> tableGet(string tableName, string key);
        Task tablePut(string tableName, Document doc);
        Task updatePut(string tableName, Document doc);
        Task deletePut(string tableName, Document doc);
    }
}
