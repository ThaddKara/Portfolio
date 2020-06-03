using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using BitExExternal.Models;

namespace BitExExternal.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class DataController : ControllerBase
    {
        /// <summary>
        /// create new user
        /// </summary>
        /// <param name="Authorization"></param>
        /// <param name="User"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        [Route("Register")]
        [HttpGet]
        public async Task<IActionResult> reg([FromHeader]string Authorization, [FromHeader]string User, [FromHeader]string Password)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Table UserTable = Table.LoadTable(amazonDynamoDBClient, "UserDetailsRegistry");
                Table PosTable = Table.LoadTable(amazonDynamoDBClient, "PositionRegistry");
                Document searchdetails = new Document();

                try
                {
                    string[] autharray = Authorization.Split(' ');
                    string authtype = autharray[0];
                    string authstring = autharray[1];

                    if (authtype == "Basic")
                    {
                        byte[] data = Convert.FromBase64String(authstring);
                        string decodedString = Encoding.UTF8.GetString(data);

                        string[] splitauth = decodedString.Split(":");
                        string id = splitauth[0];
                        string secret = splitauth[1]; Console.WriteLine(id + secret);

                        ScanOperationConfig scanOperation = new ScanOperationConfig();
                        ScanFilter scanFilter = new ScanFilter();
                        scanFilter.AddCondition("DefaultId", ScanOperator.Equal, id);
                        Search search = UserTable.Scan(scanOperation);
                        List<Document> details = await search.GetRemainingAsync();
                        foreach (Document doc in details)
                        {
                            if (doc["DefaultSecret"].ToString() == "test" && doc["DefaultId"].ToString() == "hello")
                            {
                                Console.WriteLine("successful auth");
                                searchdetails = doc;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        searchdetails["Email"].ToString();
                    }
                    else
                    {
                        return BadRequest("Bad authorization");
                    }
                }
                catch
                {
                    return BadRequest("authorizaion failed");
                }

                Document userexist = await UserTable.GetItemAsync(User);
                try
                { userexist["User"].ToString(); return BadRequest("user already exists"); }
                catch
                { }

                Document newuser = new Document();
                newuser["User"] = User;
                newuser["Password"] = Password;
                Table userreg = Table.LoadTable(amazonDynamoDBClient, "UserRegistry");
                await userreg.PutItemAsync(newuser);

                string guid = Guid.NewGuid().ToString(); Console.WriteLine(guid);
                Thread.Sleep(3);
                string gusecret = Guid.NewGuid().ToString(); Console.WriteLine(gusecret);
                Document userregdoc = new Document();
                userregdoc["Email"] = User;
                userregdoc["DefaultId"] = guid;
                userregdoc["DefaultSecret"] = gusecret;
                userregdoc["AvailableBalance"] = "1";
                await UserTable.PutItemAsync(userregdoc);

                Document posdoc = new Document();
                posdoc["Email"] = User;
                posdoc["AvailableBalance"] = "1";
                posdoc["POSSPXUSD"] = "na";
                await PosTable.PutItemAsync(posdoc);

                return Ok("user added");
            }
            catch
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// validate existing user
        /// </summary>
        /// <param name="Authorization"></param>
        /// <param name="User"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        [Route("Login")]
        [HttpGet]
        public async Task<IActionResult> login([FromHeader]string Authorization, [FromHeader]string User, [FromHeader]string Password)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Table UserTable = Table.LoadTable(amazonDynamoDBClient, "UserDetailsRegistry");
                Document searchdetails = new Document();

                try
                {
                    string[] autharray = Authorization.Split(' ');
                    string authtype = autharray[0];
                    string authstring = autharray[1];

                    if (authtype == "Basic")
                    {
                        byte[] data = Convert.FromBase64String(authstring);
                        string decodedString = Encoding.UTF8.GetString(data);

                        string[] splitauth = decodedString.Split(":");
                        string id = splitauth[0];
                        string secret = splitauth[1]; Console.WriteLine(id + secret);

                        ScanOperationConfig scanOperation = new ScanOperationConfig();
                        ScanFilter scanFilter = new ScanFilter();
                        scanFilter.AddCondition("DefaultId", ScanOperator.Equal, id);
                        Search search = UserTable.Scan(scanOperation);
                        List<Document> details = await search.GetRemainingAsync();
                        foreach (Document doc in details)
                        {
                            if (doc["DefaultSecret"].ToString() == "test" && doc["DefaultId"].ToString() == "hello")
                            {
                                Console.WriteLine("successful auth");
                                searchdetails = doc;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        searchdetails["Email"].ToString();
                    }
                    else
                    {
                        return BadRequest("Bad authorization");
                    }
                }
                catch
                {
                    return BadRequest("authorizaion failed");
                }

                Table userreg = Table.LoadTable(amazonDynamoDBClient, "UserRegistry");
                var obj = new object();

                try
                {
                    Document userregdoc = await userreg.GetItemAsync(User);
                    if (User == userregdoc["User"].ToString() && Password == userregdoc["Password"].ToString())
                    {
                        Document ret = await UserTable.GetItemAsync(User);
                        obj = new { Id = ret["DefaultId"].ToString(), Secret = ret["DefaultSecret"].ToString() };
                    }
                    else return BadRequest("fail");
                }
                catch
                {
                    return BadRequest("error");
                }

                string json = JsonConvert.SerializeObject(obj);
                return Content(json, "application/json");
            }
            catch
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// list tradeable assets
        /// </summary>
        /// <returns></returns>
        [Route("TradeableAssets")]
        [HttpGet]
        public async Task<IActionResult> assets()
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                //timestamp = timestamp - (timestamp % 5);

                AmazonDynamoDBClient client = new AmazonDynamoDBClient();
                Table assetstable = Table.LoadTable(client, "TradeablePairs");

                List<object> objarray = new List<object>();
                objarray.Add(new { Timestamp = timestamp });
                //List<Document> objarray = new List<Document>();
                try
                {
                    int i = 1;
                    while (true)
                    {
                        Document doc = await assetstable.GetItemAsync(("Pair" + i).ToString());
                        var obj = new { Pair = doc["Name"].ToString(), XBTMultiplier = doc["XBTMultiplier"].ToString() };
                        objarray.Add(obj);
                        i += 1;
                    }
                }
                catch
                { }
                
                string json = JsonConvert.SerializeObject(objarray);
                return Content(json, "application/json");
            }
            catch
            {
                return BadRequest("error");
            }
        }

        /// <summary>
        /// get open positions for user
        /// </summary>
        /// <param name="Authorization"></param>
        /// <returns></returns>
        [Route("Position")]
        [HttpGet]
        public async Task<IActionResult> positions([FromHeader]string Authorization)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Table UserTable = Table.LoadTable(amazonDynamoDBClient, "UserDetailsRegistry");
                Document searchdetails = new Document();

                try
                {
                    string[] autharray = Authorization.Split(' ');
                    string authtype = autharray[0];
                    string authstring = autharray[1];

                    if (authtype == "Basic")
                    {
                        byte[] data = Convert.FromBase64String(authstring);
                        string decodedString = Encoding.UTF8.GetString(data);

                        string[] splitauth = decodedString.Split(":");
                        string id = splitauth[0];
                        string secret = splitauth[1]; //Console.WriteLine(id + secret);

                        ScanOperationConfig scanOperation = new ScanOperationConfig();
                        ScanFilter scanFilter = new ScanFilter();
                        scanFilter.AddCondition("DefaultId", ScanOperator.Equal, id);
                        Search search = UserTable.Scan(scanOperation);
                        List<Document> details = await search.GetRemainingAsync();
                        foreach (Document doc in details)
                        {
                            if (doc["DefaultSecret"] == secret && doc["DefaultId"] == id)
                            {
                                Console.WriteLine("successful auth");
                                searchdetails = doc;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        searchdetails["Email"].ToString();
                    }
                    else
                    {
                        return BadRequest("Bad authorization");
                    }
                }
                catch
                {
                    return BadRequest("authorizaion failed");
                }

                Table PosTable = Table.LoadTable(amazonDynamoDBClient, "PositionRegistry");
                Document posdoc = await PosTable.GetItemAsync(searchdetails["Email"].ToString());
                if (posdoc["POSSPXUSD"].ToString() == "na")
                {
                    var returnobj = new { AvailableBalance = posdoc["AvailableBalance"].ToString(), POSSPXUSD = posdoc["POSSPXUSD"].ToString() };
                    string returnjson = JsonConvert.SerializeObject(returnobj);
                    return Content(returnjson, "application/json");
                }
                else
                {
                    var returnobj = new
                    {
                        AvailableBalance = posdoc["AvailableBalance"].ToString(),
                        InitialMargin = posdoc["InitialMargin"].ToString(),
                        MarginBalance = posdoc["MarginBalance"].ToString(),
                        PNL = posdoc["PNL"].ToString(),
                        PositionSPXUSD = posdoc["POSSPXUSD"].ToString(),
                        EntrySPXUSD = posdoc["POSSPXUSDEntry"].ToString(),
                        SPXValue = posdoc["SPXValue"].ToString(),
                        XBTValue = posdoc["XBTValue"].ToString()
                    };
                    string returnjson = JsonConvert.SerializeObject(returnobj);
                    return Content(returnjson, "application/json");
                }
            }
            catch
            {
                return BadRequest("error");
            }
        }
    }
}