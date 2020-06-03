using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;

namespace BolliBotIndicatorEndpoints.Services
{
    interface IScan1HourETHBTC
    {
        Task<Document> getByTimestampETH(long timestamp);
        Task<Document> getByTimestampBTC(long timestamp);

        Task<List<Document>> ETHScanBatch(List<long> timestamp);
        Task<List<Document>> BTCScanBatch(List<long> timestamp);
        Task<List<Document>> BTCScanBatchOHLCV(List<long> timestamps);
        Task<List<Document>> ETHScanBatchOHLCV(List<long> timestamps);
    }
}
