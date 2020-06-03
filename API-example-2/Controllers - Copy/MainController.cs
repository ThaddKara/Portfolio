using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;

namespace bitexinternal.Controllers
{
    [Route("")]
    [ApiController]
    public class MainController : ControllerBase
    {
        // GET: api/Main

        [HttpGet]
        public async Task<IActionResult> Get([FromHeader]string key, [FromHeader]string payload)
        {
            try
            {
                return Ok();
                if (key != "enginekey") { return BadRequest("bad"); }

                AmazonDynamoDBClient client = new AmazonDynamoDBClient();
                Table table = Table.LoadTable(client, "OrderBook");
                List<Document> docs = new List<Document>();
                int bids = payload.IndexOf("bids");
                for (int i = 0; i < payload.Length; i++)
                {
                    if (payload[i] != '[') { continue; }
                    Document doc = new Document();
                    string price = new string(""); string volume = new string("");
                    i += 1;
                    while (true && i < payload.Length)
                    {
                        if (payload[i] == ',') { i += 1; break; }
                        price += payload[i];
                        i += 1;
                    }
                    while (true && i < payload.Length)
                    {
                        if (payload[i] == ']') { break; }
                        volume += payload[i];
                        i += 1;
                    }
                    doc["Price"] = price;
                    doc["Volume"] = volume;
                    doc["Side"] = i < bids ? "Buy" : "Sell";
                    docs.Add(doc);
                }

                DocumentBatchWrite write = table.CreateBatchWrite();
                foreach (Document doc in docs)
                {
                    write.AddDocumentToPut(doc);
                }

                await write.ExecuteAsync();

                return Ok();
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }

        /// <summary>
        /// update order in database
        /// </summary>
        /// <param name="key"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("u")]
        public async Task<IActionResult> Getu([FromHeader]string key, [FromHeader]string payload)
        {
            try
            {
                if (key != "enginekey") { return BadRequest("bad"); }
                Console.WriteLine(payload);
                AmazonDynamoDBClient client = new AmazonDynamoDBClient();
                Table table = Table.LoadTable(client, "OrderBook");
                List<Document> docs = new List<Document>();
                int bids = payload.IndexOf("bids");

                int i = 0;
                if (payload[i] != '[') { throw new Exception(); }
                Document doc = new Document();
                string price = new string(""); string volume = new string(""); string type = new string("");
                i += 1;
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    price += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    volume += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ']') { break; }
                    type += payload[i];
                    i += 1;
                }
                doc["Price"] = price;
                doc["Volume"] = volume;
                doc["Type"] = type;
                await table.UpdateItemAsync(doc);

                return Ok();
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }

        /// <summary>
        /// add order to database
        /// </summary>
        /// <param name="key"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("a")]
        public async Task<IActionResult> Geta([FromHeader]string key, [FromHeader]string payload)
        {
            try
            {
                if (key != "enginekey") { return BadRequest("bad"); }
                Console.WriteLine(payload);
                AmazonDynamoDBClient client = new AmazonDynamoDBClient();
                Table table = Table.LoadTable(client, "OrderBook");
                List<Document> docs = new List<Document>();
                int bids = payload.IndexOf("bids");

                int i = 0;
                if (payload[i] != '[') { throw new Exception(); }
                Document doc = new Document();
                string price = new string(""); string volume = new string(""); string type = new string("");
                i += 1;
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    price += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    volume += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ']') { break; }
                    type += payload[i];
                    i += 1;
                }
                doc["Price"] = price;
                doc["Volume"] = volume;
                doc["Type"] = type;
                await table.PutItemAsync(doc);

                return Ok();
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }

        /// <summary>
        /// delete order from database
        /// </summary>
        /// <param name="key"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("d")]
        public async Task<IActionResult> Getd([FromHeader]string key, [FromHeader]string payload)
        {
            try
            {
                if (key != "enginekey") { return BadRequest("bad"); }
                Console.WriteLine(payload);
                AmazonDynamoDBClient client = new AmazonDynamoDBClient();
                Table table = Table.LoadTable(client, "OrderBook");
                List<Document> docs = new List<Document>();
                int bids = payload.IndexOf("bids");

                int i = 0;
                if (payload[i] != '[') { throw new Exception(); }
                Document doc = new Document();
                string price = new string(""); string volume = new string(""); string type = new string("");
                i += 1;
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    price += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    volume += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ']') { break; }
                    type += payload[i];
                    i += 1;
                }
                doc["Price"] = price;
                doc["Volume"] = volume;
                doc["Status"] = "filled";
                //doc["Type"] = type;
                await table.UpdateItemAsync(doc);

                return Ok();
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }
    }
}
