using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace bitexinternal.Controllers
{
    [Route("order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        /// <summary>
        /// executed trade
        /// </summary>
        /// <param name="key"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("e")]
        public async Task<IActionResult> Gete([FromHeader]string key, [FromHeader]string payload)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

                if (key != "enginekey") { return BadRequest("bad"); }
                Console.WriteLine(payload);
                AmazonDynamoDBClient client = new AmazonDynamoDBClient();
                Table table = Table.LoadTable(client, "OrderRegistry");
                List<Document> docs = new List<Document>();

                // begin parse payload
                int i = 0;
                if (payload[i] != '[') { throw new Exception(); }
                Document doc,docother = new Document();
                string Id = new string(""); string Quantity = new string(""); string Price = new string(""); string Executed = new string(""); string OtherOrder = new string("");
                i += 1;
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    Id += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    Quantity += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    Price += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    Executed += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ']') { break; }
                    OtherOrder += payload[i];
                    i += 1;
                }
                double executed;
                try { executed = double.Parse(Executed); } catch { return BadRequest("error parse"); }
                // end parse payload

                doc = await table.GetItemAsync(Id);
                docother = await table.GetItemAsync(OtherOrder);
                if(doc["Email"].ToString() == docother["Email"].ToString()) // self fill
                {
                    if(doc["OrderType"].ToString()=="limit")
                    {
                        double currentself = doc["OrderQuantity"].AsDouble();
                        
                        currentself = currentself - executed;
                        Document currentselfdoc = new Document();
                        currentselfdoc["Id"] = Id.ToString();
                        currentselfdoc["OrderQuantity"] = currentself.ToString();
                        await table.UpdateItemAsync(currentselfdoc);
                        return Ok();
                    }
                    return Ok();
                }

                doc["Price"] = Price;

                if(doc["OrderType"].ToString() == "market") // recent orders
                {
                    Document recentorder = new Document();
                    recentorder["Timestamp"] = timestamp.ToString();
                    recentorder["Price"] = Price.ToString();
                    recentorder["Quantity"] = Executed.ToString();
                    Table recentordertable = Table.LoadTable(client, "RecentOrdersSPXUSD");
                    await recentordertable.PutItemAsync(recentorder);
                }

                double price; double.TryParse(Price, out price);
                await table.UpdateItemAsync(doc);

                Table postable = Table.LoadTable(client, "PositionRegistry");
                Document posdoc = await postable.GetItemAsync(doc["Email"].ToString());
                Document posupdate = new Document();
                double pos;
                try
                {
                    posdoc["POSSPXUSD"].ToString();
                    if (posdoc["POSSPXUSD"].ToString() == "0") { pos = 0; }
                    pos = posdoc["POSSPXUSD"].AsDouble();
                }
                catch //never position or 0 position
                {
                    await postable.PutItemAsync(new Document() { ["Email"] = doc["Email"].ToString() });
                    pos = 0;
                }


                if (doc["Side"].ToString() == "buy" && doc["OrderType"].ToString() == "limit")
                {
                    pos = pos + executed;
                }
                else if (doc["Side"].ToString() == "sell" && doc["OrderType"].ToString() == "limit")
                {
                    pos = pos - executed;
                    executed *= -1;
                }
                else if (doc["Side"].ToString() == "sell" && doc["OrderType"].ToString() == "market")
                {
                    pos = pos - executed;
                    executed *= -1;
                }
                else if (doc["Side"].ToString() == "buy" && doc["OrderType"].ToString() == "market")
                {
                    pos = pos + executed;
                }
                else return BadRequest("error");

                posupdate["Email"] = doc["Email"].ToString();
                posupdate["POSSPXUSD"] = pos.ToString();
                //await postable.UpdateItemAsync(posupdate);

                if (pos == 0) { posupdate["POSSPXUSDEntry"] = "na"; } // closed out pos
                else if ((pos - executed) == 0 || (pos + executed) == 0 || (pos - executed) == 0 || (pos + executed) == 0) /*open pos from 0*/{ posupdate["POSSPXUSDEntry"] = Price; }
                else if ((pos - executed) > 0 || (pos + executed) < 0 || (pos - executed) < 0 || (pos + executed) > 0)/*pre-existing pos non zero*/
                {
                    if ((Math.Abs(posdoc["POSSPXUSD"].AsDouble()) > Math.Abs(executed) || (Math.Abs(posdoc["POSSPXUSD"].AsDouble()) < Math.Abs(executed) && ((posdoc["POSSPXUSD"].AsDouble() > 0 && executed > 0) || posdoc["POSSPXUSD"].AsDouble() < 0 && executed < 0)))) // reduce or add
                    {
                        if (pos > 0)
                        {
                            double newentry = ((posdoc["POSSPXUSD"].AsDouble() * posdoc["POSSPXUSDEntry"].AsDouble()) + (executed * price)) / (posdoc["POSSPXUSD"].AsDouble() + executed);
                            if ((pos > 0 && (pos - posdoc["POSSPXUSD"].AsDouble() > 0)) || (pos < 0 && (pos - posdoc["POSSPXUSD"].AsDouble() < 0)))/*if add*/ { posupdate["POSSPXUSDEntry"] = newentry.ToString(); }
                            else // profit/loss
                            { }
                        }
                        else if (pos < 0)
                        {
                            double newentry = ((posdoc["POSSPXUSD"].AsDouble() * posdoc["POSSPXUSDEntry"].AsDouble()) + (executed * price)) / (posdoc["POSSPXUSD"].AsDouble() + executed);
                            if ((pos > 0 && (pos - posdoc["POSSPXUSD"].AsDouble() > 0)) || (pos < 0 && (pos - posdoc["POSSPXUSD"].AsDouble() < 0)))/*if add*/ { posupdate["POSSPXUSDEntry"] = newentry.ToString(); }
                            else // profit/loss
                            { }
                        }
                        else { Console.WriteLine("error calc entry"); }
                    }
                    else // reduce and add
                    {
                        if (pos > 0)
                        {
                            //reduce
                            double posremain = (executed * -1) - posdoc["POSSPXUSD"].AsDouble();

                            //add
                            posupdate["POSSPXUSD"] = (0 - posremain).ToString();
                            posupdate["POSSPXUSDEntry"] = Price;
                        }
                        else if (pos < 0)
                        {
                            //reduce
                            double posremain = executed - (posdoc["POSSPXUSD"].AsDouble() * -1);

                            //add
                            posupdate["POSSPXUSD"] = (0 + posremain).ToString();
                            posupdate["POSSPXUSDEntry"] = Price;
                        }
                    }
                }

                // pull from database and calculate new position value.
                UpdateItemOperationConfig update = new UpdateItemOperationConfig();
                ExpectedValue expected = new ExpectedValue(ScanOperator.Equal);
                update.Expected = posdoc;
                try
                { Document test = await postable.UpdateItemAsync(posupdate, update); }
                catch
                {
                    Thread.Sleep(20);
                    Console.WriteLine("error");
                    posdoc = await postable.GetItemAsync(doc["Email"].ToString());
                    posupdate = new Document();

                    try
                    {
                        posdoc["POSSPXUSD"].ToString();
                        if (posdoc["POSSPXUSD"].ToString() == "0") { throw new Exception(); }
                        pos = posdoc["POSSPXUSD"].AsDouble();
                    }
                    catch //never position or 0 position
                    {
                        pos = 0;
                    }

                    try { executed = double.Parse(Executed); } catch { return BadRequest("error parse"); }

                    if (doc["Side"].ToString() == "buy" && doc["OrderType"].ToString() == "limit")
                    {
                        pos = pos + executed;
                    }
                    else if (doc["Side"].ToString() == "sell" && doc["OrderType"].ToString() == "limit")
                    {
                        pos = pos - executed;
                        executed *= -1;
                    }
                    else if (doc["Side"].ToString() == "sell" && doc["OrderType"].ToString() == "market")
                    {
                        pos = pos - executed;
                        executed *= -1;
                    }
                    else if (doc["Side"].ToString() == "buy" && doc["OrderType"].ToString() == "market")
                    {
                        pos = pos + executed;
                    }
                    else return BadRequest("error");

                    posupdate["Email"] = doc["Email"].ToString();
                    posupdate["POSSPXUSD"] = pos.ToString();

                    if (pos == 0) { posupdate["POSSPXUSDEntry"] = "na"; } // closed out pos
                    else if ((pos - executed) == 0 || (pos + executed) == 0 || (pos - executed) == 0 || (pos + executed) == 0) /*open pos from 0*/{ posupdate["POSSPXUSDEntry"] = Price; }
                    else if ((pos - executed) > 0 || (pos + executed) < 0 || (pos - executed) < 0 || (pos + executed) > 0)/*pre-existing pos non zero*/
                    {
                        if ((Math.Abs(posdoc["POSSPXUSD"].AsDouble()) > Math.Abs(executed) || (Math.Abs(posdoc["POSSPXUSD"].AsDouble()) < Math.Abs(executed) && ((posdoc["POSSPXUSD"].AsDouble() > 0 && executed > 0) || posdoc["POSSPXUSD"].AsDouble() < 0 && executed < 0)))) // reduce or add
                        {
                            if (pos > 0)
                            {
                                double newentry = ((posdoc["POSSPXUSD"].AsDouble() * posdoc["POSSPXUSDEntry"].AsDouble()) + (executed * price)) / (posdoc["POSSPXUSD"].AsDouble() + executed);
                                if ((pos > 0 && (pos - posdoc["POSSPXUSD"].AsDouble() > 0)) || (pos < 0 && (pos - posdoc["POSSPXUSD"].AsDouble() < 0)))/*if add*/ { posupdate["POSSPXUSDEntry"] = newentry.ToString(); }
                                else // profit/loss
                                { }
                            }
                            else if (pos < 0)
                            {
                                double newentry = ((posdoc["POSSPXUSD"].AsDouble() * posdoc["POSSPXUSDEntry"].AsDouble()) + (executed * price)) / (posdoc["POSSPXUSD"].AsDouble() + executed);
                                if ((pos > 0 && (pos - posdoc["POSSPXUSD"].AsDouble() > 0)) || (pos < 0 && (pos - posdoc["POSSPXUSD"].AsDouble() < 0)))/*if add*/ { posupdate["POSSPXUSDEntry"] = newentry.ToString(); }
                                else // profit/loss
                                { }
                            }
                            else { Console.WriteLine("error calc entry"); }
                        }
                        else // reduce and add
                        {
                            if (pos > 0)
                            {
                                //reduce
                                double posremain = (executed * -1) - posdoc["POSSPXUSD"].AsDouble();

                                //add
                                posupdate["POSSPXUSD"] = (0 - posremain).ToString();
                                posupdate["POSSPXUSDEntry"] = Price;
                            }
                            else if (pos < 0)
                            {
                                //reduce
                                double posremain = executed - (posdoc["POSSPXUSD"].AsDouble() * -1);

                                //add
                                posupdate["POSSPXUSD"] = (0 + posremain).ToString();
                                posupdate["POSSPXUSDEntry"] = Price;
                            }
                        }
                    }

                    await postable.UpdateItemAsync(posupdate);
                }

                return Ok();
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }

        /// <summary>
        /// update existing order for user
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
                Table table = Table.LoadTable(client, "OrderRegistry");
                List<Document> docs = new List<Document>();

                //begin parse
                int i = 0;
                if (payload[i] != '[') { throw new Exception(); }
                Document doc = new Document();
                string Id = new string(""); string Quantity = new string("");
                i += 1;
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    Id += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ']') {break; }
                    Quantity += payload[i];
                    i += 1;
                }
                //end parse

                doc["Id"] = Id;
                doc["OrderQuantity"] = Quantity;
                
                await table.UpdateItemAsync(doc);

                return Ok();
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }
        [HttpGet]
        [Route("a")]
        public async Task<IActionResult> Geta([FromHeader]string key, [FromHeader]string payload)
        {
            try
            {
                return Ok();
                if (key != "enginekey") { return BadRequest("bad"); }
                Console.WriteLine(payload);
                AmazonDynamoDBClient client = new AmazonDynamoDBClient();
                Table table = Table.LoadTable(client, "OrderRegistry");
                List<Document> docs = new List<Document>();

                int i = 0;
                if (payload[i] != '[') { throw new Exception(); }
                Document doc = new Document();
                string Id = new string(""); string Quantity = new string("");
                i += 1;
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    Id += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ']') { break; }
                    Quantity += payload[i];
                    i += 1;
                }

                doc["Id"] = Id;
                doc["OrderQuantity"] = Quantity;

                await table.UpdateItemAsync(doc);

                return Ok();
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }
        [HttpGet]
        [Route("d")]
        public async Task<IActionResult> Getd([FromHeader]string key, [FromHeader]string payload)
        {
            try
            {
                if (key != "enginekey") { return BadRequest("bad"); }
                Console.WriteLine(payload);
                AmazonDynamoDBClient client = new AmazonDynamoDBClient();
                Table table = Table.LoadTable(client, "OrderRegistry");
                List<Document> docs = new List<Document>();

                int i = 0;
                if (payload[i] != '[') { throw new Exception(); }
                Document doc = new Document();
                string Id = new string(""); string Quantity = new string("");
                i += 1;
                while (true && i < payload.Length)
                {
                    if (payload[i] == ',') { i += 1; break; }
                    Id += payload[i];
                    i += 1;
                }
                while (true && i < payload.Length)
                {
                    if (payload[i] == ']') { break; }
                    Quantity += payload[i];
                    i += 1;
                }

                doc["Id"] = Id;
                doc["OrderQuantity"] = Quantity;

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