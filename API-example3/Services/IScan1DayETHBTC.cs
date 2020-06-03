using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;

namespace BolliBotIndicatorEndpoints.Services
{
    interface IScan1DayETHBTC
    {
        Task<Document> scanBTC(long timestamp);
        Task<Document> scanETH(long timestamp);
        Task<List<Document>> ETHScanBatch(List<long> timestamp);
        Task<List<Document>> BTCScanBatch(List<long> timestamp);
        Task<List<Document>> BTCScanBatchOHLCV(List<long> timestamps);
        Task<List<Document>> ETHScanBatchOHLCV(List<long> timestamps);
    }
}
