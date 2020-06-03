using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using BolliBotIndicatorEndpoints.Services;
using Newtonsoft.Json;
using Amazon.APIGateway;
using Amazon.APIGateway.Model;
using Microsoft.Extensions.Primitives;
using BolliBotIndicatorEndpoints.Models;
using Newtonsoft.Json.Linq;

namespace BolliBotIndicatorEndpoints.Controllers
{
    [Route("v1/eth")]
    [ApiController]
    public class ETHIndicatorsSummaryController : ControllerBase
    {
        [Route("book")]
        [HttpGet]
        public async Task<IActionResult> GetBook([FromQuery]string exchange)
        {
            try
            {
                long unixTimestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                long unixTimestamp5min = unixTimestamp - DateTime.UtcNow.Minute % 5 * 60;
                unixTimestamp5min -= DateTime.UtcNow.Second;
                unixTimestamp5min *= 1000;
                long unixTimestamp5minNext = unixTimestamp5min - (300 * 1000);

                AmazonDynamoDBClient amazonDynamoDB = new AmazonDynamoDBClient();
                ETH5MinOrderBook orderBooks = new ETH5MinOrderBook(amazonDynamoDB);

                var request = Request;
                var headers = Request.Headers;
                StringValues stringValues = default(StringValues);
                headers.TryGetValue("X-API-KEY", out stringValues);

                // verify X-API-KEY
                try
                {
                    APIKeys keys = new APIKeys(amazonDynamoDB);
                    Document keyInfo = await keys.getAPIKey(stringValues);
                    if ((long)keyInfo["Tier"] < 2) { return BadRequest("tier 2 required for this endpoint"); }
                }
                catch
                {
                    return BadRequest("an error occured");
                }

                try
                {
                    if (exchange == "bitmex")
                    {
                        string str = await orderBooks.getJson(unixTimestamp5min.ToString(), exchange);
                        return Content(str, "application/json");
                    }
                    else if (exchange == "deribit")
                    {
                        string str = await orderBooks.getJson(unixTimestamp5min.ToString(), exchange);
                        return Content(str, "application/json");
                    }
                    else
                    { return BadRequest("bad exchange"); }
                }
                catch
                {
                    if (exchange == "bitmex")
                    {
                        string str = await orderBooks.getJson(unixTimestamp5minNext.ToString(), exchange);
                        return Content(str, "application/json");
                    }
                    else if (exchange == "deribit")
                    {
                        string str = await orderBooks.getJson(unixTimestamp5minNext.ToString(), exchange);
                        return Content(str, "application/json");
                    }
                    else
                    { return BadRequest("bad exchange"); }
                }
            }
            catch
            {
                return BadRequest("an error occured");
            }
        }

        [Route("summary")]
        [HttpGet]
        public async Task<IActionResult> GetSummary([FromQuery]long interval, [FromQuery]string exchange)
        {
            try
            {
                long unixTimestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                long unixTimestamp1day = unixTimestamp - DateTime.UtcNow.Hour * 60 * 60;
                unixTimestamp1day -= DateTime.UtcNow.Minute * 60;
                unixTimestamp1day -= DateTime.UtcNow.Second;
                unixTimestamp1day *= 1000;

                long unixTimestamp1hour = unixTimestamp - DateTime.UtcNow.Minute * 60;
                unixTimestamp1hour -= DateTime.UtcNow.Second;
                unixTimestamp1hour *= 1000;

                long unixTimestamp5min = unixTimestamp - DateTime.UtcNow.Minute % 5 * 60;
                unixTimestamp5min -= DateTime.UtcNow.Second;
                unixTimestamp5min *= 1000;

                long unixTimestamp5minNext = unixTimestamp5min - (300 * 1000);
                long unixTimestamp1hourNext = unixTimestamp1hour - (3600 * 1000);
                long unixTimestamp1dayNext = unixTimestamp1day - (86400 * 1000);

                AmazonDynamoDBClient amazonDynamoDB = new AmazonDynamoDBClient();
                Scan5MinETHBTC scan5MinETHBTC = new Scan5MinETHBTC(amazonDynamoDB);
                Scan1HourETHBTC scan1HourETHBTC = new Scan1HourETHBTC(amazonDynamoDB);
                Scan1DayETHBTC scan1DayETHBTC = new Scan1DayETHBTC(amazonDynamoDB);

                var request = Request;
                var headers = Request.Headers;
                StringValues stringValues = default(StringValues);
                headers.TryGetValue("X-API-KEY", out stringValues);

                // verify X-API-KEY
                try
                {
                    APIKeys keys = new APIKeys(amazonDynamoDB);
                    Document keyInfo = await keys.getAPIKey(stringValues);
                    if ((long)keyInfo["Tier"] < 1) { return BadRequest("tier 1 required for this endpoint"); }
                }
                catch
                {
                    return BadRequest("an error occured");
                }

                if (exchange == "bitmex")
                {
                    try
                    {
                        if (interval == 300)
                        {
                            Document doc = await scan5MinETHBTC.ETHScan(unixTimestamp5min);

                            SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                (float)doc[$"{"Bitmex"}200ma"], (float)doc[$"{"Bitmex"}150ma"],
                                (float)doc[$"{"Bitmex"}100ma"], (float)doc[$"{"Bitmex"}50ma"],
                                (float)doc[$"{"Bitmex"}20ma"], (float)doc[$"{"Bitmex"}200ema"],
                                (float)doc[$"{"Bitmex"}150ema"], (float)doc[$"{"Bitmex"}100ema"],
                                (float)doc[$"{"Bitmex"}50ema"], (float)doc[$"{"Bitmex"}20ema"],
                                (float)doc[$"{"Bitmex"}rsiavggain"], (float)doc[$"{"Bitmex"}rsiavgloss"],
                                (float)doc[$"{"Bitmex"}rsi"], (float)doc[$"{"Bitmex"}upperbb"],
                                (float)doc[$"{"Bitmex"}middlebb"], (float)doc[$"{"Bitmex"}lowerbb"],
                                (float)doc[$"{"Bitmex"}percentb"]);

                            return Content(JsonConvert.SerializeObject(summary), "application/json");
                        }
                        else if (interval == 3600)
                        {
                            Document doc = await scan1HourETHBTC.getByTimestampETH(unixTimestamp1hour);

                            SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                (float)doc[$"{"Bitmex"}200ma"], (float)doc[$"{"Bitmex"}150ma"],
                                (float)doc[$"{"Bitmex"}100ma"], (float)doc[$"{"Bitmex"}50ma"],
                                (float)doc[$"{"Bitmex"}20ma"], (float)doc[$"{"Bitmex"}200ema"],
                                (float)doc[$"{"Bitmex"}150ema"], (float)doc[$"{"Bitmex"}100ema"],
                                (float)doc[$"{"Bitmex"}50ema"], (float)doc[$"{"Bitmex"}20ema"],
                                (float)doc[$"{"Bitmex"}rsiavggain"], (float)doc[$"{"Bitmex"}rsiavgloss"],
                                (float)doc[$"{"Bitmex"}rsi"], (float)doc[$"{"Bitmex"}upperbb"],
                                (float)doc[$"{"Bitmex"}middlebb"], (float)doc[$"{"Bitmex"}lowerbb"],
                                (float)doc[$"{"Bitmex"}percentb"]);

                            return Content(JsonConvert.SerializeObject(summary), "application/json");
                        }
                        else if (interval == 86400)
                        {
                            Document doc = await scan1DayETHBTC.scanETH(unixTimestamp1day);

                            SummaryModel1Day summary = new SummaryModel1Day((long)doc[$"Timestamp"],
                                (float)doc[$"{"Bitmex"}50ma"],
                                (float)doc[$"{"Bitmex"}20ma"], (float)doc[$"{"Bitmex"}50ema"],
                                (float)doc[$"{"Bitmex"}20ema"],
                                (float)doc[$"{"Bitmex"}rsiavggain"], (float)doc[$"{"Bitmex"}rsiavgloss"],
                                (float)doc[$"{"Bitmex"}rsi"], (float)doc[$"{"Bitmex"}upperbb"],
                                (float)doc[$"{"Bitmex"}middlebb"], (float)doc[$"{"Bitmex"}lowerbb"],
                                (float)doc[$"{"Bitmex"}percentb"]);

                            return Content(JsonConvert.SerializeObject(summary), "application/json");
                        }
                        else
                        {
                            return BadRequest("bad interval");
                        }
                    }
                    catch
                    {
                        try
                        {
                            if (interval == 300)
                            {
                                Document doc = await scan5MinETHBTC.ETHScan(unixTimestamp5minNext);

                                SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                    (float)doc[$"{"Bitmex"}200ma"], (float)doc[$"{"Bitmex"}150ma"],
                                    (float)doc[$"{"Bitmex"}100ma"], (float)doc[$"{"Bitmex"}50ma"],
                                    (float)doc[$"{"Bitmex"}20ma"], (float)doc[$"{"Bitmex"}200ema"],
                                    (float)doc[$"{"Bitmex"}150ema"], (float)doc[$"{"Bitmex"}100ema"],
                                    (float)doc[$"{"Bitmex"}50ema"], (float)doc[$"{"Bitmex"}20ema"],
                                    (float)doc[$"{"Bitmex"}rsiavggain"], (float)doc[$"{"Bitmex"}rsiavgloss"],
                                    (float)doc[$"{"Bitmex"}rsi"], (float)doc[$"{"Bitmex"}upperbb"],
                                    (float)doc[$"{"Bitmex"}middlebb"], (float)doc[$"{"Bitmex"}lowerbb"],
                                    (float)doc[$"{"Bitmex"}percentb"]);

                                return Content(JsonConvert.SerializeObject(summary), "application/json");
                            }
                            else if (interval == 3600)
                            {
                                Document doc = await scan1HourETHBTC.getByTimestampETH(unixTimestamp1hourNext);

                                SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                    (float)doc[$"{"Bitmex"}200ma"], (float)doc[$"{"Bitmex"}150ma"],
                                    (float)doc[$"{"Bitmex"}100ma"], (float)doc[$"{"Bitmex"}50ma"],
                                    (float)doc[$"{"Bitmex"}20ma"], (float)doc[$"{"Bitmex"}200ema"],
                                    (float)doc[$"{"Bitmex"}150ema"], (float)doc[$"{"Bitmex"}100ema"],
                                    (float)doc[$"{"Bitmex"}50ema"], (float)doc[$"{"Bitmex"}20ema"],
                                    (float)doc[$"{"Bitmex"}rsiavggain"], (float)doc[$"{"Bitmex"}rsiavgloss"],
                                    (float)doc[$"{"Bitmex"}rsi"], (float)doc[$"{"Bitmex"}upperbb"],
                                    (float)doc[$"{"Bitmex"}middlebb"], (float)doc[$"{"Bitmex"}lowerbb"],
                                    (float)doc[$"{"Bitmex"}percentb"]);

                                return Content(JsonConvert.SerializeObject(summary), "application/json");
                            }
                            else if (interval == 86400)
                            {
                                Document doc = await scan1DayETHBTC.scanETH(unixTimestamp1dayNext);

                                SummaryModel1Day summary = new SummaryModel1Day((long)doc[$"Timestamp"],
                                    (float)doc[$"{"Bitmex"}50ma"],
                                    (float)doc[$"{"Bitmex"}20ma"], (float)doc[$"{"Bitmex"}50ema"],
                                    (float)doc[$"{"Bitmex"}20ema"],
                                    (float)doc[$"{"Bitmex"}rsiavggain"], (float)doc[$"{"Bitmex"}rsiavgloss"],
                                    (float)doc[$"{"Bitmex"}rsi"], (float)doc[$"{"Bitmex"}upperbb"],
                                    (float)doc[$"{"Bitmex"}middlebb"], (float)doc[$"{"Bitmex"}lowerbb"],
                                    (float)doc[$"{"Bitmex"}percentb"]);

                                return Content(JsonConvert.SerializeObject(summary), "application/json");
                            }
                            else
                            {
                                return BadRequest("bad interval");
                            }
                        }
                        catch
                        {
                            return BadRequest("unknown error");
                        }
                    }
                }
                else if (exchange == "deribit")
                {
                    try
                    {
                        if (interval == 300)
                        {
                            Document doc = await scan5MinETHBTC.ETHScan(unixTimestamp5min);

                            SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                (float)doc[$"{"Deribit"}200ma"], (float)doc[$"{"Deribit"}150ma"],
                                (float)doc[$"{"Deribit"}100ma"], (float)doc[$"{"Deribit"}50ma"],
                                (float)doc[$"{"Deribit"}20ma"], (float)doc[$"{"Deribit"}200ema"],
                                (float)doc[$"{"Deribit"}150ema"], (float)doc[$"{"Deribit"}100ema"],
                                (float)doc[$"{"Deribit"}50ema"], (float)doc[$"{"Deribit"}20ema"],
                                (float)doc[$"{"Deribit"}rsiavggain"], (float)doc[$"{"Deribit"}rsiavgloss"],
                                (float)doc[$"{"Deribit"}rsi"], (float)doc[$"{"Deribit"}upperbb"],
                                (float)doc[$"{"Deribit"}middlebb"], (float)doc[$"{"Deribit"}lowerbb"],
                                (float)doc[$"{"Deribit"}percentb"]);

                            return Content(JsonConvert.SerializeObject(summary), "application/json");
                        }
                        else if (interval == 3600)
                        {
                            Document doc = await scan1HourETHBTC.getByTimestampETH(unixTimestamp1hour);

                            SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                (float)doc[$"{"Deribit"}200ma"], (float)doc[$"{"Deribit"}150ma"],
                                (float)doc[$"{"Deribit"}100ma"], (float)doc[$"{"Deribit"}50ma"],
                                (float)doc[$"{"Deribit"}20ma"], (float)doc[$"{"Deribit"}200ema"],
                                (float)doc[$"{"Deribit"}150ema"], (float)doc[$"{"Deribit"}100ema"],
                                (float)doc[$"{"Deribit"}50ema"], (float)doc[$"{"Deribit"}20ema"],
                                (float)doc[$"{"Deribit"}rsiavggain"], (float)doc[$"{"Deribit"}rsiavgloss"],
                                (float)doc[$"{"Deribit"}rsi"], (float)doc[$"{"Deribit"}upperbb"],
                                (float)doc[$"{"Deribit"}middlebb"], (float)doc[$"{"Deribit"}lowerbb"],
                                (float)doc[$"{"Deribit"}percentb"]);

                            return Content(JsonConvert.SerializeObject(summary), "application/json");
                        }
                        else if (interval == 86400)
                        {
                            Document doc = await scan1DayETHBTC.scanETH(unixTimestamp1day);

                            SummaryModel1Day summary = new SummaryModel1Day((long)doc[$"Timestamp"],
                                (float)doc[$"{"Deribit"}50ma"],
                                (float)doc[$"{"Deribit"}20ma"], (float)doc[$"{"Deribit"}50ema"],
                                (float)doc[$"{"Deribit"}20ema"],
                                (float)doc[$"{"Deribit"}rsiavggain"], (float)doc[$"{"Deribit"}rsiavgloss"],
                                (float)doc[$"{"Deribit"}rsi"], (float)doc[$"{"Deribit"}upperbb"],
                                (float)doc[$"{"Deribit"}middlebb"], (float)doc[$"{"Deribit"}lowerbb"],
                                (float)doc[$"{"Deribit"}percentb"]);

                            return Content(JsonConvert.SerializeObject(summary), "application/json");
                        }
                        else
                        {
                            return BadRequest("bad interval");
                        }
                    }
                    catch
                    {
                        try
                        {
                            if (interval == 300)
                            {
                                Document doc = await scan5MinETHBTC.ETHScan(unixTimestamp5minNext);

                                SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                    (float)doc[$"{"Deribit"}200ma"], (float)doc[$"{"Deribit"}150ma"],
                                    (float)doc[$"{"Deribit"}100ma"], (float)doc[$"{"Deribit"}50ma"],
                                    (float)doc[$"{"Deribit"}20ma"], (float)doc[$"{"Deribit"}200ema"],
                                    (float)doc[$"{"Deribit"}150ema"], (float)doc[$"{"Deribit"}100ema"],
                                    (float)doc[$"{"Deribit"}50ema"], (float)doc[$"{"Deribit"}20ema"],
                                    (float)doc[$"{"Deribit"}rsiavggain"], (float)doc[$"{"Deribit"}rsiavgloss"],
                                    (float)doc[$"{"Deribit"}rsi"], (float)doc[$"{"Deribit"}upperbb"],
                                    (float)doc[$"{"Deribit"}middlebb"], (float)doc[$"{"Deribit"}lowerbb"],
                                    (float)doc[$"{"Deribit"}percentb"]);

                                return Content(JsonConvert.SerializeObject(summary), "application/json");
                            }
                            else if (interval == 3600)
                            {
                                Document doc = await scan1HourETHBTC.getByTimestampETH(unixTimestamp1hourNext);

                                SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                    (float)doc[$"{"Deribit"}200ma"], (float)doc[$"{"Deribit"}150ma"],
                                    (float)doc[$"{"Deribit"}100ma"], (float)doc[$"{"Deribit"}50ma"],
                                    (float)doc[$"{"Deribit"}20ma"], (float)doc[$"{"Deribit"}200ema"],
                                    (float)doc[$"{"Deribit"}150ema"], (float)doc[$"{"Deribit"}100ema"],
                                    (float)doc[$"{"Deribit"}50ema"], (float)doc[$"{"Deribit"}20ema"],
                                    (float)doc[$"{"Deribit"}rsiavggain"], (float)doc[$"{"Deribit"}rsiavgloss"],
                                    (float)doc[$"{"Deribit"}rsi"], (float)doc[$"{"Deribit"}upperbb"],
                                    (float)doc[$"{"Deribit"}middlebb"], (float)doc[$"{"Deribit"}lowerbb"],
                                    (float)doc[$"{"Deribit"}percentb"]);

                                return Content(JsonConvert.SerializeObject(summary), "application/json");
                            }
                            else if (interval == 86400)
                            {
                                Document doc = await scan1DayETHBTC.scanETH(unixTimestamp1dayNext);

                                SummaryModel1Day summary = new SummaryModel1Day((long)doc[$"Timestamp"],
                                    (float)doc[$"{"Deribit"}50ma"],
                                    (float)doc[$"{"Deribit"}20ma"], (float)doc[$"{"Deribit"}50ema"],
                                    (float)doc[$"{"Deribit"}20ema"],
                                    (float)doc[$"{"Deribit"}rsiavggain"], (float)doc[$"{"Deribit"}rsiavgloss"],
                                    (float)doc[$"{"Deribit"}rsi"], (float)doc[$"{"Deribit"}upperbb"],
                                    (float)doc[$"{"Deribit"}middlebb"], (float)doc[$"{"Deribit"}lowerbb"],
                                    (float)doc[$"{"Deribit"}percentb"]);

                                return Content(JsonConvert.SerializeObject(summary), "application/json");
                            }
                            else
                            {
                                return BadRequest("bad interval");
                            }
                        }
                        catch
                        {
                            return BadRequest("unknown error");
                        }
                    }
                }
                else
                {
                    return BadRequest("bad exchange");
                }
            }
            catch
            {
                return BadRequest("an error occured");
            }
        }

        [Route("historic")]
        [HttpGet]
        public async Task<IActionResult> GetHistoric([FromQuery]long interval, [FromQuery]string exchange, [FromQuery]long start, [FromQuery]long limit)
        {
            try
            {
                const long limit1day = 50;
                const long limit1hour = 300;
                const long limit5min = 500;

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Scan5MinETHBTC scan5Min = new Scan5MinETHBTC(amazonDynamoDBClient);
                Scan1HourETHBTC scan1Hour = new Scan1HourETHBTC(amazonDynamoDBClient);
                Scan1DayETHBTC scan1Day = new Scan1DayETHBTC(amazonDynamoDBClient);

                long unixTimestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                long unixTimestamp1day = unixTimestamp - DateTime.UtcNow.Hour * 60 * 60;
                unixTimestamp1day -= DateTime.UtcNow.Minute * 60;
                unixTimestamp1day -= DateTime.UtcNow.Second;
                unixTimestamp1day *= 1000;

                long unixTimestamp1hour = unixTimestamp - DateTime.UtcNow.Minute * 60;
                unixTimestamp1hour -= DateTime.UtcNow.Second;
                unixTimestamp1hour *= 1000;

                long unixTimestamp5min = unixTimestamp - DateTime.UtcNow.Minute % 5 * 60;
                unixTimestamp5min -= DateTime.UtcNow.Second;
                unixTimestamp5min *= 1000;

                long unixTimestamp5minNext = unixTimestamp5min - (300 * 1000);
                long unixTimestamp1hourNext = unixTimestamp1hour - (3600 * 1000);
                long unixTimestamp1dayNext = unixTimestamp1day - (86400 * 1000);

                if (interval == 300 && limit > limit5min) { limit = limit5min; } // set 5 min limit
                if (interval == 3600 && limit > limit1hour) { limit = limit1hour; } // set 1 hour limit
                if (interval == 8600 && limit > limit1day) { limit = limit1day; } // set 1 day limit

                var request = Request;
                var headers = Request.Headers;
                StringValues stringValues = default(StringValues);
                headers.TryGetValue("X-API-KEY", out stringValues);

                // verify X-API-KEY
                try
                {
                    APIKeys keys = new APIKeys(amazonDynamoDBClient);
                    Document keyInfo = await keys.getAPIKey(stringValues);
                    if ((long)keyInfo["Tier"] < 1) { return BadRequest("tier 1 required for this endpoint"); }
                }
                catch
                {
                    return BadRequest("an error occured");
                }

                if (exchange == "bitmex")
                {
                    if (interval == 300)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, SummaryModel> summaryModels = new Dictionary<long, SummaryModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp5min) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (300 * 1000);
                            }

                            List<Document> summaries = await scan5Min.ETHScanBatch(timestamps);

                            foreach (Document doc in summaries)
                            {
                                SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                (float)doc[$"{"Bitmex"}200ma"], (float)doc[$"{"Bitmex"}150ma"],
                                (float)doc[$"{"Bitmex"}100ma"], (float)doc[$"{"Bitmex"}50ma"],
                                (float)doc[$"{"Bitmex"}20ma"], (float)doc[$"{"Bitmex"}200ema"],
                                (float)doc[$"{"Bitmex"}150ema"], (float)doc[$"{"Bitmex"}100ema"],
                                (float)doc[$"{"Bitmex"}50ema"], (float)doc[$"{"Bitmex"}20ema"],
                                (float)doc[$"{"Bitmex"}rsiavggain"], (float)doc[$"{"Bitmex"}rsiavgloss"],
                                (float)doc[$"{"Bitmex"}rsi"], (float)doc[$"{"Bitmex"}upperbb"],
                                (float)doc[$"{"Bitmex"}middlebb"], (float)doc[$"{"Bitmex"}lowerbb"],
                                (float)doc[$"{"Bitmex"}percentb"]);

                                summaryModels.Add((long)doc[$"Timestamp"], summary);
                            }

                            SortedDictionary<long, SummaryModel> sortedModels = new SortedDictionary<long, SummaryModel>(summaryModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else if (interval == 3600)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, SummaryModel> summaryModels = new Dictionary<long, SummaryModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp1hour) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (3600 * 1000);
                            }

                            List<Document> summaries = await scan1Hour.ETHScanBatch(timestamps);

                            foreach (Document doc in summaries)
                            {
                                SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                (float)doc[$"{"Bitmex"}200ma"], (float)doc[$"{"Bitmex"}150ma"],
                                (float)doc[$"{"Bitmex"}100ma"], (float)doc[$"{"Bitmex"}50ma"],
                                (float)doc[$"{"Bitmex"}20ma"], (float)doc[$"{"Bitmex"}200ema"],
                                (float)doc[$"{"Bitmex"}150ema"], (float)doc[$"{"Bitmex"}100ema"],
                                (float)doc[$"{"Bitmex"}50ema"], (float)doc[$"{"Bitmex"}20ema"],
                                (float)doc[$"{"Bitmex"}rsiavggain"], (float)doc[$"{"Bitmex"}rsiavgloss"],
                                (float)doc[$"{"Bitmex"}rsi"], (float)doc[$"{"Bitmex"}upperbb"],
                                (float)doc[$"{"Bitmex"}middlebb"], (float)doc[$"{"Bitmex"}lowerbb"],
                                (float)doc[$"{"Bitmex"}percentb"]);

                                summaryModels.Add((long)doc[$"Timestamp"], summary);
                            }

                            SortedDictionary<long, SummaryModel> sortedModels = new SortedDictionary<long, SummaryModel>(summaryModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else if (interval == 86400)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, SummaryModel1Day> summaryModels = new Dictionary<long, SummaryModel1Day>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp1day) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (86400 * 1000);
                            }

                            List<Document> summaries = await scan1Day.ETHScanBatch(timestamps);

                            foreach (Document doc in summaries)
                            {
                                SummaryModel1Day summary = new SummaryModel1Day((long)doc[$"Timestamp"],
                                    (float)doc[$"{"Bitmex"}50ma"],
                                    (float)doc[$"{"Bitmex"}20ma"], (float)doc[$"{"Bitmex"}50ema"],
                                    (float)doc[$"{"Bitmex"}20ema"],
                                    (float)doc[$"{"Bitmex"}rsiavggain"], (float)doc[$"{"Bitmex"}rsiavgloss"],
                                    (float)doc[$"{"Bitmex"}rsi"], (float)doc[$"{"Bitmex"}upperbb"],
                                    (float)doc[$"{"Bitmex"}middlebb"], (float)doc[$"{"Bitmex"}lowerbb"],
                                    (float)doc[$"{"Bitmex"}percentb"]);

                                summaryModels.Add((long)doc[$"Timestamp"], summary);
                            }

                            SortedDictionary<long, SummaryModel1Day> sortedModels = new SortedDictionary<long, SummaryModel1Day>(summaryModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else
                    {
                        return BadRequest("bad interval");
                    }
                }
                else if (exchange == "deribit")
                {
                    if (interval == 300)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, SummaryModel> summaryModels = new Dictionary<long, SummaryModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp5min) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (300 * 1000);
                            }

                            List<Document> summaries = await scan5Min.ETHScanBatch(timestamps);

                            foreach (Document doc in summaries)
                            {
                                SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                (float)doc[$"{"Deribit"}200ma"], (float)doc[$"{"Deribit"}150ma"],
                                (float)doc[$"{"Deribit"}100ma"], (float)doc[$"{"Deribit"}50ma"],
                                (float)doc[$"{"Deribit"}20ma"], (float)doc[$"{"Deribit"}200ema"],
                                (float)doc[$"{"Deribit"}150ema"], (float)doc[$"{"Deribit"}100ema"],
                                (float)doc[$"{"Deribit"}50ema"], (float)doc[$"{"Deribit"}20ema"],
                                (float)doc[$"{"Deribit"}rsiavggain"], (float)doc[$"{"Deribit"}rsiavgloss"],
                                (float)doc[$"{"Deribit"}rsi"], (float)doc[$"{"Deribit"}upperbb"],
                                (float)doc[$"{"Deribit"}middlebb"], (float)doc[$"{"Deribit"}lowerbb"],
                                (float)doc[$"{"Deribit"}percentb"]);

                                summaryModels.Add((long)doc[$"Timestamp"], summary);
                            }

                            SortedDictionary<long, SummaryModel> sortedModels = new SortedDictionary<long, SummaryModel>(summaryModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else if (interval == 3600)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, SummaryModel> summaryModels = new Dictionary<long, SummaryModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp1hour) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (3600 * 1000);
                            }

                            List<Document> summaries = await scan1Hour.ETHScanBatch(timestamps);

                            foreach (Document doc in summaries)
                            {
                                SummaryModel summary = new SummaryModel((long)doc[$"Timestamp"],
                                (float)doc[$"{"Deribit"}200ma"], (float)doc[$"{"Deribit"}150ma"],
                                (float)doc[$"{"Deribit"}100ma"], (float)doc[$"{"Deribit"}50ma"],
                                (float)doc[$"{"Deribit"}20ma"], (float)doc[$"{"Deribit"}200ema"],
                                (float)doc[$"{"Deribit"}150ema"], (float)doc[$"{"Deribit"}100ema"],
                                (float)doc[$"{"Deribit"}50ema"], (float)doc[$"{"Deribit"}20ema"],
                                (float)doc[$"{"Deribit"}rsiavggain"], (float)doc[$"{"Deribit"}rsiavgloss"],
                                (float)doc[$"{"Deribit"}rsi"], (float)doc[$"{"Deribit"}upperbb"],
                                (float)doc[$"{"Deribit"}middlebb"], (float)doc[$"{"Deribit"}lowerbb"],
                                (float)doc[$"{"Deribit"}percentb"]);

                                summaryModels.Add((long)doc[$"Timestamp"], summary);
                            }

                            SortedDictionary<long, SummaryModel> sortedModels = new SortedDictionary<long, SummaryModel>(summaryModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else if (interval == 86400)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, SummaryModel1Day> summaryModels = new Dictionary<long, SummaryModel1Day>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp1day) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (86400 * 1000);
                            }

                            List<Document> summaries = await scan1Day.ETHScanBatch(timestamps);

                            foreach (Document doc in summaries)
                            {
                                SummaryModel1Day summary = new SummaryModel1Day((long)doc[$"Timestamp"],
                                    (float)doc[$"{"Deribit"}50ma"],
                                    (float)doc[$"{"Deribit"}20ma"], (float)doc[$"{"Deribit"}50ema"],
                                    (float)doc[$"{"Deribit"}20ema"],
                                    (float)doc[$"{"Deribit"}rsiavggain"], (float)doc[$"{"Deribit"}rsiavgloss"],
                                    (float)doc[$"{"Deribit"}rsi"], (float)doc[$"{"Deribit"}upperbb"],
                                    (float)doc[$"{"Deribit"}middlebb"], (float)doc[$"{"Deribit"}lowerbb"],
                                    (float)doc[$"{"Deribit"}percentb"]);

                                summaryModels.Add((long)doc[$"Timestamp"], summary);
                            }

                            SortedDictionary<long, SummaryModel1Day> sortedModels = new SortedDictionary<long, SummaryModel1Day>(summaryModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else
                    {
                        return BadRequest("bad interval");
                    }
                }
                else
                {
                    return BadRequest("bad exchange");
                }
            }
            catch
            {
                return BadRequest("an error occured");
            }
        }

        [Route("ohlcv")]
        [HttpGet]
        public async Task<IActionResult> GetOHLCV([FromQuery]long interval, [FromQuery]string exchange, [FromQuery]long start, [FromQuery]long limit)
        {

            try
            {
                const long limit1day = 50;
                const long limit1hour = 300;
                const long limit5min = 500;

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Scan5MinETHBTC scan5Min = new Scan5MinETHBTC(amazonDynamoDBClient);
                Scan1HourETHBTC scan1Hour = new Scan1HourETHBTC(amazonDynamoDBClient);
                Scan1DayETHBTC scan1Day = new Scan1DayETHBTC(amazonDynamoDBClient);

                long unixTimestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                long unixTimestamp1day = unixTimestamp - DateTime.UtcNow.Hour * 60 * 60;
                unixTimestamp1day -= DateTime.UtcNow.Minute * 60;
                unixTimestamp1day -= DateTime.UtcNow.Second;
                unixTimestamp1day *= 1000;

                long unixTimestamp1hour = unixTimestamp - DateTime.UtcNow.Minute * 60;
                unixTimestamp1hour -= DateTime.UtcNow.Second;
                unixTimestamp1hour *= 1000;

                long unixTimestamp5min = unixTimestamp - DateTime.UtcNow.Minute % 5 * 60;
                unixTimestamp5min -= DateTime.UtcNow.Second;
                unixTimestamp5min *= 1000;

                long unixTimestamp5minNext = unixTimestamp5min - (300 * 1000);
                long unixTimestamp1hourNext = unixTimestamp1hour - (3600 * 1000);
                long unixTimestamp1dayNext = unixTimestamp1day - (86400 * 1000);

                if (interval == 300 && limit > limit5min) { limit = limit5min; } // set 5 min limit
                if (interval == 3600 && limit > limit1hour) { limit = limit1hour; } // set 1 hour limit
                if (interval == 8600 && limit > limit1day) { limit = limit1day; } // set 1 day limit

                var request = Request;
                var headers = Request.Headers;
                StringValues stringValues = default(StringValues);
                headers.TryGetValue("X-API-KEY", out stringValues);

                // verify X-API-KEY
                try
                {
                    APIKeys keys = new APIKeys(amazonDynamoDBClient);
                    Document keyInfo = await keys.getAPIKey(stringValues);
                    if ((long)keyInfo["Tier"] < 1) { return BadRequest("tier 1 required for this endpoint"); }
                }
                catch
                {
                    return BadRequest("an error occured");
                }

                if (exchange == "bitmex")
                {
                    if (interval == 300)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, OHLCVModel> ohlcvModels = new Dictionary<long, OHLCVModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp5min) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (300 * 1000);
                            }

                            List<Document> summaries = await scan5Min.ETHScanBatchOHLCV(timestamps);

                            foreach (Document doc in summaries)
                            {
                                string ohlcv = JsonConvert.SerializeObject(doc["Bitmex"]);
                                JArray temp = JArray.Parse(doc["Bitmex"].ToString());
                                //temp.ToArray();
                                OHLCVModel OHLCVmodel = new OHLCVModel((double)temp[0], (double)temp[1], (double)temp[2], (double)temp[3], (double)temp[4]);

                                ohlcvModels.Add((long)doc[$"Timestamp"], OHLCVmodel);
                            }

                            SortedDictionary<long, OHLCVModel> sortedModels = new SortedDictionary<long, OHLCVModel>(ohlcvModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else if (interval == 3600)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, OHLCVModel> ohlcvModels = new Dictionary<long, OHLCVModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp1hour) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (3600 * 1000);
                            }

                            List<Document> summaries = await scan1Hour.ETHScanBatchOHLCV(timestamps);

                            foreach (Document doc in summaries)
                            {
                                string ohlcv = JsonConvert.SerializeObject(doc["Bitmex"]);
                                JArray temp = JArray.Parse(doc["Bitmex"].ToString());
                                //temp.ToArray();
                                OHLCVModel OHLCVmodel = new OHLCVModel((double)temp[0], (double)temp[1], (double)temp[2], (double)temp[3], (double)temp[4]);

                                ohlcvModels.Add((long)doc[$"Timestamp"], OHLCVmodel);
                            }

                            SortedDictionary<long, OHLCVModel> sortedModels = new SortedDictionary<long, OHLCVModel>(ohlcvModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else if (interval == 86400)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, OHLCVModel> ohlcvModels = new Dictionary<long, OHLCVModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp1day) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (86400 * 1000);
                            }

                            List<Document> summaries = await scan1Day.ETHScanBatchOHLCV(timestamps);

                            foreach (Document doc in summaries)
                            {
                                string ohlcv = JsonConvert.SerializeObject(doc["Bitmex"]);
                                JArray temp = JArray.Parse(doc["Bitmex"].ToString());
                                //temp.ToArray();
                                OHLCVModel OHLCVmodel = new OHLCVModel((double)temp[0], (double)temp[1], (double)temp[2], (double)temp[3], (double)temp[4]);

                                ohlcvModels.Add((long)doc[$"Timestamp"], OHLCVmodel);
                            }

                            SortedDictionary<long, OHLCVModel> sortedModels = new SortedDictionary<long, OHLCVModel>(ohlcvModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else
                    {
                        return BadRequest("bad interval");
                    }
                }
                else if (exchange == "deribit")
                {
                    if (interval == 300)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, OHLCVModel> ohlcvModels = new Dictionary<long, OHLCVModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp5min) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (300 * 1000);
                            }

                            List<Document> summaries = await scan5Min.ETHScanBatchOHLCV(timestamps);

                            foreach (Document doc in summaries)
                            {
                                string ohlcv = JsonConvert.SerializeObject(doc["Bitmex"]);
                                JArray temp = JArray.Parse(doc["Deribit"].ToString());
                                //temp.ToArray();
                                OHLCVModel OHLCVmodel = new OHLCVModel((double)temp[0], (double)temp[1], (double)temp[2], (double)temp[3], (double)temp[4]);

                                ohlcvModels.Add((long)doc[$"Timestamp"], OHLCVmodel);
                            }

                            SortedDictionary<long, OHLCVModel> sortedModels = new SortedDictionary<long, OHLCVModel>(ohlcvModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else if (interval == 3600)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, OHLCVModel> ohlcvModels = new Dictionary<long, OHLCVModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp1hour) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (3600 * 1000);
                            }

                            List<Document> summaries = await scan1Hour.ETHScanBatchOHLCV(timestamps);

                            foreach (Document doc in summaries)
                            {
                                string ohlcv = JsonConvert.SerializeObject(doc["Bitmex"]);
                                JArray temp = JArray.Parse(doc["Deribit"].ToString());
                                //temp.ToArray();
                                OHLCVModel OHLCVmodel = new OHLCVModel((double)temp[0], (double)temp[1], (double)temp[2], (double)temp[3], (double)temp[4]);

                                ohlcvModels.Add((long)doc[$"Timestamp"], OHLCVmodel);
                            }

                            SortedDictionary<long, OHLCVModel> sortedModels = new SortedDictionary<long, OHLCVModel>(ohlcvModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else if (interval == 86400)
                    {
                        try
                        {
                            List<long> timestamps = new List<long>();
                            Dictionary<long, OHLCVModel> ohlcvModels = new Dictionary<long, OHLCVModel>();

                            long localtimestamp = start;
                            for (int i = 0; i < limit; i++)
                            {
                                if (localtimestamp == unixTimestamp1day) { timestamps.Add(localtimestamp); break; }

                                timestamps.Add(localtimestamp);
                                localtimestamp += (86400 * 1000);
                            }

                            List<Document> summaries = await scan1Day.ETHScanBatchOHLCV(timestamps);

                            foreach (Document doc in summaries)
                            {
                                string ohlcv = JsonConvert.SerializeObject(doc["Bitmex"]);
                                JArray temp = JArray.Parse(doc["Deribit"].ToString());
                                //temp.ToArray();
                                OHLCVModel OHLCVmodel = new OHLCVModel((double)temp[0], (double)temp[1], (double)temp[2], (double)temp[3], (double)temp[4]);

                                ohlcvModels.Add((long)doc[$"Timestamp"], OHLCVmodel);
                            }

                            SortedDictionary<long, OHLCVModel> sortedModels = new SortedDictionary<long, OHLCVModel>(ohlcvModels);

                            return Content(JsonConvert.SerializeObject(sortedModels), "application/json");
                        }
                        catch
                        {
                            return BadRequest("error finding start");
                        }
                    }
                    else
                    {
                        return BadRequest("bad interval");
                    }
                }
                else
                {
                    return BadRequest("bad exchange");
                }
            }
            catch
            {
                return BadRequest("an error occured");
            }
        }

        [Route("updates")]
        [HttpGet]
        public async Task<IActionResult> Update([FromQuery]long interval, [FromQuery]string exchange, [FromQuery]string source)
        {
            var request = Request;
            var headers = Request.Headers;
            StringValues stringValues = default(StringValues);
            headers.TryGetValue("X-API-KEY", out stringValues);

            string savedSource;

            AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();

            // verify api key
            try
            {
                APIKeys keys = new APIKeys(amazonDynamoDBClient);
                Document keyInfo = await keys.getAPIKey(stringValues);
                if ((long)keyInfo["Tier"] < 2) { return BadRequest("tier 2 required for this endpoint"); }
            }
            catch
            {
                return BadRequest("an error occured");
            }

            if (interval != 300 && interval != 3600 && interval != 86400) { return BadRequest("bad interval"); }

            try
            {
                GenericGet generic = new GenericGet(amazonDynamoDBClient);
                Document doc = await generic.tableGet("UpdateList", stringValues);
                if (string.IsNullOrWhiteSpace(source)) { savedSource = doc["Source"]; }
                else { savedSource = source; doc["Source"] = source; }
            }
            catch
            {
                GenericGet generic = new GenericGet(amazonDynamoDBClient);
                Document doc = new Document();
                doc["Key"] = stringValues.ToString();
                doc["Source"] = source.ToString();
                await generic.tablePut("UpdateList", doc);
                savedSource = source;
            }

            try
            {
                if (exchange == "bitmex")
                {
                    GenericGet generic = new GenericGet(amazonDynamoDBClient);
                    Document doc = new Document();
                    doc["Key"] = stringValues.ToString();
                    doc["Source"] = savedSource;
                    doc[$"ETHbitmex{interval.ToString()}"] = "true";

                    await generic.updatePut("UpdateList", doc);

                    return Content($"success subscribing to ETHbitmex{interval.ToString()}", "application/json");
                }
                else if (exchange == "deribit")
                {
                    GenericGet generic = new GenericGet(amazonDynamoDBClient);
                    Document doc = new Document();
                    doc["Key"] = stringValues.ToString();
                    doc["Source"] = savedSource;
                    doc[$"ETHderibit{interval.ToString()}"] = "true";

                    await generic.updatePut("UpdateList", doc);

                    return Content($"success subscribing to ETHderibit{interval.ToString()}", "application/json");
                }
                else
                {
                    return BadRequest("bad exchange");
                }
            }
            catch
            {
                return BadRequest("an error occured");
            }
        }

        [Route("updates/remove")]
        [HttpGet]
        public async Task<IActionResult> RemoveUpdates()
        {
            var request = Request;
            var headers = Request.Headers;
            StringValues stringValues = default(StringValues);
            headers.TryGetValue("X-API-KEY", out stringValues);

            string savedSource;

            AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();

            // verify api key
            try
            {
                APIKeys keys = new APIKeys(amazonDynamoDBClient);
                Document keyInfo = await keys.getAPIKey(stringValues);
                if ((long)keyInfo["Tier"] < 2) { return BadRequest("tier 2 required for this endpoint"); }
            }
            catch
            {
                return BadRequest("an error occured");
            }

            try
            {
                GenericGet generic = new GenericGet(amazonDynamoDBClient);

                Document updateDoc = await generic.tableGet("UpdateList", stringValues.ToString());

                Document doc = new Document();
                doc["Key"] = stringValues.ToString();
                //doc["Source"] = updateDoc["Source"].ToString();

                await generic.tablePut("UpdateList", doc);

                return Content("success removing update subscriptions", "application/json");
            }
            catch
            {
                return BadRequest("an error occured");
            }
        }

        [Route("events")]
        [HttpGet]
        public async Task<IActionResult> Subscribe([FromQuery]long interval, [FromQuery]string exchange, [FromQuery]string eventtype, [FromQuery]string source)
        {
            var request = Request;
            var headers = Request.Headers;
            StringValues stringValues = default(StringValues);
            headers.TryGetValue("X-API-KEY", out stringValues);

            string savedSource;

            AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();

            // verify api key
            try
            {
                APIKeys keys = new APIKeys(amazonDynamoDBClient);
                Document keyInfo = await keys.getAPIKey(stringValues);
                if ((long)keyInfo["Tier"] < 2) { return BadRequest("tier 2 required for this endpoint"); }
            }
            catch
            {
                return BadRequest("an error occured");
            }

            if (interval != 300 && interval != 3600 && interval != 86400) { return BadRequest("bad interval"); }

            try
            {
                GenericGet generic = new GenericGet(amazonDynamoDBClient);
                Document doc = await generic.tableGet("SubscribeList", stringValues);
                if (string.IsNullOrWhiteSpace(source)) { savedSource = doc["Source"]; }
                else { savedSource = source; }
            }
            catch
            {
                GenericGet generic = new GenericGet(amazonDynamoDBClient);
                Document doc = new Document();
                doc["Key"] = stringValues.ToString();
                doc["Source"] = source.ToString();
                await generic.tablePut("SubscribeList", doc);
                savedSource = source;
            }

            try
            {
                if (exchange == "bitmex")
                {
                    if (eventtype == "macrossma")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHbitmex{interval.ToString()}macrossma"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHbitmex{interval.ToString()}macrossma", "application/json");
                    }
                    else if (eventtype == "closecrossma")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHbitmex{interval.ToString()}closecrossma"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHbitmex{interval.ToString()}closecrossma", "application/json");
                    }
                    else if (eventtype == "macrosstf") // defunct
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHbitmex{interval.ToString()}macrosstf"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHbitmex{interval.ToString()}macrosstf", "application/json");
                    }
                    else if (eventtype == "emacrossema")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHbitmex{interval.ToString()}emacrossema"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHbitmex{interval.ToString()}emacrossema", "application/json");
                    }
                    else if (eventtype == "closecrossema")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHbitmex{interval.ToString()}closecrossema"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHbitmex{interval.ToString()}closecrossema", "application/json");
                    }
                    else if (eventtype == "emacrosstf") // defunct
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHbitmex{interval.ToString()}emacrosstf"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHbitmex{interval.ToString()}emacrosstf", "application/json");
                    }
                    else if (eventtype == "emacrossma")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHbitmex{interval.ToString()}emacrossma"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHbitmex{interval.ToString()}emacrossma", "application/json");
                    }
                    else if (eventtype == "breakbb")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHbitmex{interval.ToString()}breakbb"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHbitmex{interval.ToString()}breakbb", "application/json");
                    }
                    else if (eventtype == "rsi")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHbitmex{interval.ToString()}rsi"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHbitmex{interval.ToString()}rsi", "application/json");
                    }
                    else
                    {
                        return BadRequest("bad eventtype");
                    }
                }
                else if (exchange == "deribit")
                {

                    if (eventtype == "macrossma")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHderibit{interval.ToString()}macrossma"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHderibit{interval.ToString()}macrossma", "application/json");
                    }
                    else if (eventtype == "closecrossma")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHderibit{interval.ToString()}closecrossma"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHderibit{interval.ToString()}closecrossma", "application/json");
                    }
                    else if (eventtype == "macrosstf") // defunct
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHderibit{interval.ToString()}macrosstf"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHderibit{interval.ToString()}macrosstf", "application/json");
                    }
                    else if (eventtype == "emacrossema")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHderibit{interval.ToString()}emacrossema"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHderibit{interval.ToString()}emacrossema", "application/json");
                    }
                    else if (eventtype == "closecrossema")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHderibit{interval.ToString()}closecrossema"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHderibit{interval.ToString()}closecrossema", "application/json");
                    }
                    else if (eventtype == "emacrosstf") // defunct
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHderibit{interval.ToString()}emacrosstf"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHderibit{interval.ToString()}emacrosstf", "application/json");
                    }
                    else if (eventtype == "emacrossma")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHderibit{interval.ToString()}emacrossma"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHderibit{interval.ToString()}emacrossma", "application/json");
                    }
                    else if (eventtype == "breakbb")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHderibit{interval.ToString()}breakbb"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHderibit{interval.ToString()}breakbb", "application/json");
                    }
                    else if (eventtype == "rsi")
                    {
                        GenericGet generic = new GenericGet(amazonDynamoDBClient);
                        Document doc = new Document();
                        doc["Key"] = stringValues.ToString();
                        doc["Source"] = savedSource;
                        doc[$"ETHderibit{interval.ToString()}rsi"] = "true";

                        await generic.updatePut("SubscribeList", doc);

                        return Content($"success subscribing to ETHderibit{interval.ToString()}rsi", "application/json");
                    }
                    else
                    {
                        return BadRequest("bad eventtype");
                    }
                }
                else
                {
                    return BadRequest("bad exchange");
                }
            }
            catch
            {
                return BadRequest("an error occured");
            }
        }

        [Route("events/remove")]
        [HttpGet]
        public async Task<IActionResult> removeEvents()
        {
            var request = Request;
            var headers = Request.Headers;
            StringValues stringValues = default(StringValues);
            headers.TryGetValue("X-API-KEY", out stringValues);

            string savedSource;

            AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();

            // verify api key
            try
            {
                APIKeys keys = new APIKeys(amazonDynamoDBClient);
                Document keyInfo = await keys.getAPIKey(stringValues);
                if ((long)keyInfo["Tier"] < 2) { return BadRequest("tier 2 required for this endpoint"); }
            }
            catch
            {
                return BadRequest("an error occured");
            }

            try
            {
                GenericGet generic = new GenericGet(amazonDynamoDBClient);

                Document updateDoc = await generic.tableGet("SubscribeList", stringValues.ToString());

                Document doc = new Document();
                doc["Key"] = stringValues.ToString();
                //doc["Source"] = updateDoc["Source"].ToString();

                await generic.tablePut("SubscribeList", doc);

                return Content("success removing event subscriptions", "application/json");
            }
            catch
            {
                return BadRequest("an error occured");
            }
        }
    }
}