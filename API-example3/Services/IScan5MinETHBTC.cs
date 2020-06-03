using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;

namespace BolliBotIndicatorEndpoints.Services
{
    public interface IScan5MinETHBTC
    {
        Task<Document> ETHScan(long timestamp);
        Task<Document> BTCScan(long timestamp);
        Task<List<Document>> ETHScanBatch(List<long> timestamps);
        Task<List<Document>> BTCScanBatch(List<long> timestamps);
        Task<List<Document>> BTCScanBatchOHLCV(List<long> timestamps);
        Task<List<Document>> ETHScanBatchOHLCV(List<long> timestamps);
    }
}
