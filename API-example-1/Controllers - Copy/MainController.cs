using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using System.Text;
using System.Net.Sockets;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using BitExExternal.Models;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace BitexExternal.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class MainController : ControllerBase
    {
        int port = 2222;

        /// <summary>
        /// place orders
        /// </summary>
        /// <param name="Authorization">authorization key</param>
        /// <param name="orderPost">json post data</param>
        /// <returns></returns>
        [Route("Order")]
        [HttpPost]
        public async Task<IActionResult> PostOrder([FromHeader]string Authorization, [FromBody]OrderPost orderPost)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                long markpricetimestamp = timestamp;// - DateTime.UtcNow.Second;

                if (Request.ContentType == "application/json")
                {; }
                else
                {
                    return BadRequest("improper content type");
                }

                if (orderPost.OrderType == "limit") { if (string.IsNullOrEmpty(orderPost.Contract) && string.IsNullOrEmpty(orderPost.OrderType) && string.IsNullOrEmpty(orderPost.Price) && orderPost.OrderQuantity == null) { return BadRequest("bad params"); } }
                else if (orderPost.OrderType == "market") { if (string.IsNullOrEmpty(orderPost.Contract) && string.IsNullOrEmpty(orderPost.OrderType) && orderPost.OrderQuantity == null) { return BadRequest("bad params"); } }
                else { return BadRequest("missing params"); }

                Console.WriteLine(orderPost.Contract + orderPost.OrderQuantity + orderPost.OrderType + orderPost.Side);
                if (string.IsNullOrEmpty(orderPost.Contract) || string.IsNullOrEmpty(orderPost.OrderType) || string.IsNullOrEmpty(orderPost.Side) || orderPost.OrderQuantity == null)
                {
                    return BadRequest("missing variables");
                }

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Table UserTable = Table.LoadTable(amazonDynamoDBClient, "UserDetailsRegistry");
                Table SPXMarkTable = Table.LoadTable(amazonDynamoDBClient, "SPXMarkPrice");
                Document searchdetails = new Document();

                orderPost.OrderType = orderPost.OrderType.ToLower();

                int orderId;
                string socket;

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

                    // verifiy valid account balance
                    Document userdetails = await UserTable.GetItemAsync(searchdetails["Email"].ToString());
                    Document markprice = new Document();
                    markpricetimestamp = markpricetimestamp - (markpricetimestamp % 5);
                    try { markprice = await SPXMarkTable.GetItemAsync(markpricetimestamp.ToString()); markprice["BTCPrice"].ToString(); }
                    catch { try { markpricetimestamp -= 5; markprice = await SPXMarkTable.GetItemAsync((markpricetimestamp).ToString()); markprice["BTCPrice"].ToString(); } catch { Console.WriteLine("bigbad"); } }

                    double availablebalance = searchdetails["AvailableBalance"].AsDouble();
                    if (orderPost.OrderQuantity > ((availablebalance * markprice["BTCPrice"].AsDouble()) * 100))
                    {
                        return BadRequest("bad amount");
                    }

                    // atomic id generate
                    Table idgenerator = Table.LoadTable(amazonDynamoDBClient, "IdGenerator");
                    Document currentid = await idgenerator.GetItemAsync("Main");
                    Document newid = new Document();
                    newid["Id"] = "Main";

                    double count = currentid["Count"].AsDouble();
                    count += 1;
                    newid["Count"] = count.ToString();

                    UpdateItemOperationConfig updateItemOperationConfig = new UpdateItemOperationConfig();
                    ExpectedValue expected = new ExpectedValue(ScanOperator.Equal);
                    updateItemOperationConfig.Expected = currentid;

                    try
                    {
                        Document test = await idgenerator.UpdateItemAsync(newid, updateItemOperationConfig);
                        orderId = (int)count;
                    }
                    catch
                    {
                        return BadRequest("please try again");
                    }

                    Table orderreg = Table.LoadTable(amazonDynamoDBClient, "OrderRegistry");
                    Document addorder = new Document();
                    addorder["Id"] = orderId.ToString();
                    addorder["Email"] = searchdetails["Email"].ToString();
                    addorder["Side"] = orderPost.Side.ToString();
                    if (orderPost.OrderType == "limit") { addorder["Price"] = orderPost.Price.ToString(); }
                    addorder["OrderQuantity"] = orderPost.OrderQuantity.ToString();
                    addorder["Contract"] = orderPost.Contract;
                    addorder["OrderType"] = orderPost.OrderType;
                    addorder["Status"] = "open";
                    addorder["Timestamp"] = timestamp.ToString();
                    //addorder["Add"] = "true";
                    await orderreg.PutItemAsync(addorder);

                    // send tcp message to engine
                    try
                    {
                        TcpClient tcpclnt = new TcpClient();
                        Console.WriteLine("Connecting.....");

                        tcpclnt.Connect("52.213.34.99", port);
                        // use the ipaddress as in the server program

                        Console.WriteLine("Connected");
                        Console.Write("Enter the string to be transmitted : ");
                        var enginepayload = new object();
                        double spymark = markprice["SPXPrice"].AsDouble();
                        spymark = Math.Round(spymark);
                        spymark *= 100;

                        if (orderPost.OrderType == "limit")
                        {
                            enginepayload = new
                            {
                                Method = "Post",
                                Side = orderPost.Side,
                                Id = orderId.ToString(),
                                Price = orderPost.Price,
                                OrderQuantity = orderPost.OrderQuantity.ToString(),
                                OrderType = orderPost.OrderType,
                                Contract = orderPost.Contract,
                                Secret = "secret",
                                //Timestamp = timestamp.ToString(),
                                //User = searchdetails["Email"].ToString(),
                                //AvailableBalance = searchdetails["AvailableBalance"].ToString()
                            };
                        }
                        else if (orderPost.OrderType == "market")
                        {
                            enginepayload = new
                            {
                                Method = "Post",
                                Side = orderPost.Side,
                                Id = orderId.ToString(),
                                OrderQuantity = orderPost.OrderQuantity.ToString(),
                                Slippage = 99999.ToString(),
                                OrderType = orderPost.OrderType,
                                Contract = orderPost.Contract,
                                Secret = "secret",
                                //Timestamp = timestamp.ToString(),
                                //User = searchdetails["Email"].ToString(),
                                //AvailableBalance = searchdetails["AvailableBalance"].ToString()
                            };
                        }
                        else { return BadRequest("invalid order type"); }

                        using (SslStream sslStream = new SslStream(tcpclnt.GetStream(), false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                        {
                            sslStream.AuthenticateAsClient("52.213.34.99");
                            // This is where you read and send data

                            String str = "enginekey" + JsonConvert.SerializeObject(enginepayload);
                            //Stream stm = tcpclnt.GetStream();

                            ASCIIEncoding asen = new ASCIIEncoding();
                            byte[] ba = asen.GetBytes(str);
                            Console.WriteLine("Transmitting.....");

                            sslStream.Write(ba, 0, ba.Length);
                            //sslStream.Close();

                            /*byte[] bb = new byte[1000];
                            int k = await sslStream.ReadAsync(bb, 0, 1000);

                            var socketresult = Encoding.UTF8.GetString(bb).TrimEnd('\0');
                            Console.WriteLine(socketresult);
                            socket = socketresult;*/
                        }
                        tcpclnt.Close();
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine("Error..... " + e.StackTrace);
                        return BadRequest("error with engine");
                    }

                }
                catch
                {
                    return BadRequest("failed processing");
                }

                var returnobj = new { Id = orderId, Timestamp = markpricetimestamp.ToString(), Message = "transmitted", Result = "sucess" };
                string returnjson = JsonConvert.SerializeObject(returnobj);
                return Content(returnjson, "application/json");
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }

        /// <summary>
        /// get open orders
        /// </summary>
        /// <param name="Authorization">authorization key</param>
        /// <param name="Contract">(optional)retrieve orders for specified contract</param>
        /// <param name="OrderId">(optional)retrieve order for specified Id</param>
        /// <returns></returns>
        [Route("Order")]
        [HttpGet]
        public async Task<IActionResult> GetOrder([FromHeader]string Authorization, [FromQuery]string Contract = null, [FromQuery]string OrderId = null)
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

                Table ordertable = Table.LoadTable(amazonDynamoDBClient, "OrderRegistry");

                if (OrderId != null)
                {
                    try
                    {
                        Document singleorder = await ordertable.GetItemAsync(OrderId);
                        if (singleorder["Email"].ToString() != searchdetails["Email"])
                        { throw new Exception(); }

                        var returnobj = new
                        {
                            Id = OrderId,
                            Contract = singleorder["Contract"].ToString(),
                            OrderQuantity = singleorder["OrderQuantity"].AsDouble(),
                            OrderType = singleorder["OrderType"].ToString(),
                            Price = singleorder["Price"].AsDouble(),
                            Timestamp = timestamp.ToString()
                        };
                        string returnstring = JsonConvert.SerializeObject(returnobj);

                        return Content(returnstring, "application/json");
                    }
                    catch
                    {
                        return BadRequest("order not found");
                    }
                }

                ScanOperationConfig scanOperationConfig = new ScanOperationConfig();
                ScanFilter filter = new ScanFilter();
                filter.AddCondition("Email", ScanOperator.Equal, searchdetails["Email"].ToString());
                if (Contract != null) { filter.AddCondition("Contract", ScanOperator.Equal, Contract); }
                scanOperationConfig.Filter = filter;

                Search searchorder = ordertable.Scan(scanOperationConfig);
                List<Document> orders = await searchorder.GetRemainingAsync();

                if (orders.Count == 0)
                { return Ok("no orders found"); }

                List<OrderGet> orderlist = new List<OrderGet>();
                foreach (Document doc in orders)
                {
                    orderlist.Add(new OrderGet { Id = doc["Id"], Contract = doc["Contract"].ToString(), OrderQuantity = doc["OrderQuantity"].AsDouble(), OrderType = doc["OrderType"].ToString(), Price = doc["Price"].AsDouble() });
                }

                string returnjson = JsonConvert.SerializeObject(orderlist);

                return Content(returnjson, "application/json");
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }

        /// <summary>
        /// delete orders
        /// </summary>
        /// <param name="Authorization">authorization key</param>
        /// <param name="orderDelete">json payload containing parameters</param>
        /// <returns></returns>
        [Route("Order")]
        [HttpDelete]
        public async Task<IActionResult> DeleteOrder([FromHeader]string Authorization, [FromBody]OrderDelete orderDelete)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                if (orderDelete.OrderId == null)
                {
                    return BadRequest("missing variables");
                }

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Table UserTable = Table.LoadTable(amazonDynamoDBClient, "UserDetailsRegistry");
                Document searchdetails = new Document();

                string socket;

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

                Table ordertable = Table.LoadTable(amazonDynamoDBClient, "OrderRegistry");

                try
                {
                    Document deletedoc = await ordertable.GetItemAsync(orderDelete.OrderId);
                    if (deletedoc["Email"].ToString() != searchdetails["Email"].ToString())
                    {
                        return BadRequest("order not found");
                    }

                    deletedoc["Modify"] = "true";
                    await ordertable.UpdateItemAsync(deletedoc);

                    /*deletedoc = await ordertable.DeleteItemAsync(orderDelete.OrderId);
                    var returnobj = new { Id = orderDelete.OrderId, Message = "delete successful", Timestamp = timestamp.ToString() };
                    string returnstring = JsonConvert.SerializeObject(returnobj);*/

                    // send tcp message to engine
                    try
                    {
                        TcpClient tcpclnt = new TcpClient();
                        Console.WriteLine("Connecting.....");

                        tcpclnt.Connect("52.213.34.99", port);
                        // use the ipaddress as in the server program

                        Console.WriteLine("Connected");
                        Console.Write("Enter the string to be transmitted : ");

                        var enginepayload = new { Method = "Delete", Id = orderDelete.OrderId, User = searchdetails["Email"].ToString(), AvailableBalance = searchdetails["AvailableBalance"].ToString() };
                        using (SslStream sslStream = new SslStream(tcpclnt.GetStream(), false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                        {
                            sslStream.AuthenticateAsClient("52.213.34.99");
                            // This is where you read and send data

                            String str = "enginekey" + JsonConvert.SerializeObject(enginepayload);
                            //Stream stm = tcpclnt.GetStream();

                            ASCIIEncoding asen = new ASCIIEncoding();
                            byte[] ba = asen.GetBytes(str);
                            Console.WriteLine("Transmitting.....");

                            sslStream.Write(ba, 0, ba.Length);
                            sslStream.Close();

                            /*byte[] bb = new byte[1000];
                            int k = await sslStream.ReadAsync(bb, 0, 1000);

                            var socketresult = Encoding.UTF8.GetString(bb).TrimEnd('\0');
                            Console.WriteLine(socketresult);
                            socket = socketresult;*/
                        }
                        tcpclnt.Close();
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine("Error..... " + e.StackTrace);
                        return BadRequest("error with engine");
                    }

                    var returnobj = new { Timestamp = timestamp.ToString(), Result = "sucess" };
                    string returnjson = JsonConvert.SerializeObject(returnobj);
                    return Content(returnjson, "application/json");
                }
                catch
                {
                    return BadRequest("order not found");
                }
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }

        /// <summary>
        /// change orders
        /// </summary>
        /// <param name="Authorization">authorization key</param>
        /// <param name="orderPut">payload containing parameters</param>
        /// <returns></returns>
        [Route("Order")]
        [HttpPut]
        public async Task<IActionResult> PutOrder([FromHeader]string Authorization, [FromBody]OrderPut orderPut)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                if (orderPut.OrderId == null || orderPut.NewPrice == null || orderPut.NewQuantity == null)
                {
                    return BadRequest("missing variables");
                }

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Table UserTable = Table.LoadTable(amazonDynamoDBClient, "UserDetailsRegistry");
                Document searchdetails = new Document();

                string socket;

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

                Table ordertable = Table.LoadTable(amazonDynamoDBClient, "OrderRegistry");

                try
                {
                    Document putdoc = await ordertable.GetItemAsync(orderPut.OrderId);
                    if (putdoc["Email"].ToString() != searchdetails["Email"].ToString())
                    {
                        return BadRequest("order not found");
                    }

                    putdoc["Modify"] = "true";
                    await ordertable.UpdateItemAsync(putdoc);
                }
                catch
                {
                    return BadRequest("order not found");
                }

                // send tcp message to engine
                try
                {
                    TcpClient tcpclnt = new TcpClient();
                    Console.WriteLine("Connecting.....");

                    tcpclnt.Connect("52.213.34.99", port);
                    // use the ipaddress as in the server program

                    Console.WriteLine("Connected");
                    Console.Write("Enter the string to be transmitted : ");


                    var enginepayload = new
                    {
                        Method = "Put",
                        Id = orderPut.OrderId,
                        NewPrice = orderPut.NewPrice,
                        NewQuantity = orderPut.NewQuantity,
                        User = searchdetails["Email"].ToString(),
                        Secret = "secret",
                        AvailableBalance = searchdetails["AvailableBalance"].ToString()
                    };
                    using (SslStream sslStream = new SslStream(tcpclnt.GetStream(), false,
                         new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                    {
                        sslStream.AuthenticateAsClient("52.213.34.99");
                        // This is where you read and send data

                        String str = "enginekey" + JsonConvert.SerializeObject(enginepayload);
                        //Stream stm = tcpclnt.GetStream();

                        ASCIIEncoding asen = new ASCIIEncoding();
                        byte[] ba = asen.GetBytes(str);
                        Console.WriteLine("Transmitting.....");

                        sslStream.Write(ba, 0, ba.Length);
                        sslStream.Close();

                        /*byte[] bb = new byte[1000];
                        int k = await sslStream.ReadAsync(bb, 0, 1000);

                        var socketresult = Encoding.UTF8.GetString(bb).TrimEnd('\0');
                        Console.WriteLine(socketresult);
                        socket = socketresult;*/
                    }
                    tcpclnt.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error..... " + e.StackTrace);
                    return BadRequest("error with engine");
                }

                var returnobj = new { Result = "sucess" };
                string returnjson = JsonConvert.SerializeObject(returnobj);
                return Content(returnjson, "application/json");
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }

        /// <summary>
        /// special admin endpoint for sending custom messages to TCP server
        /// </summary>
        /// <param name="Authorization">admin authorization key</param>
        /// <param name="orderAdmin">payload</param>
        /// <returns></returns>
        [Route("admin")]
        [HttpPost]
        public async Task<IActionResult> admin([FromHeader]string Authorization, [FromBody]OrderAdmin orderAdmin)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Table UserTable = Table.LoadTable(amazonDynamoDBClient, "UserDetailsRegistry");
                Document searchdetails = new Document();

                string socket;

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

                        Document doc = await UserTable.GetItemAsync("testemail");
                        if (id == doc["DefaultId"].ToString() && secret == doc["DefaultSecret"].ToString())
                        {
                            //ok
                            searchdetails = doc;
                        }
                        else
                        { return BadRequest(); }
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
                // send tcp message to engine
                try
                {
                    TcpClient tcpclnt = new TcpClient();
                    Console.WriteLine("Connecting.....");

                    tcpclnt.Connect("52.213.34.99", port);
                    // use the ipaddress as in the server program

                    Console.WriteLine("Connected");
                    Console.Write("Enter the string to be transmitted : ");

                    var enginepayload = new
                    {
                        Method = "Admin",
                        Admin = orderAdmin.Admin
                    };
                    using (SslStream sslStream = new SslStream(tcpclnt.GetStream(), false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                    {
                        sslStream.AuthenticateAsClient("52.213.34.99");
                        // This is where you read and send data

                        String str = "enginekey" + JsonConvert.SerializeObject(enginepayload);
                        //Stream stm = tcpclnt.GetStream();

                        ASCIIEncoding asen = new ASCIIEncoding();
                        byte[] ba = asen.GetBytes(str);
                        Console.WriteLine("Transmitting.....");

                        sslStream.Write(ba, 0, ba.Length);
                        sslStream.Close();

                        /*byte[] bb = new byte[1000];
                        int k = await sslStream.ReadAsync(bb, 0, 1000);

                        var socketresult = Encoding.UTF8.GetString(bb).TrimEnd('\0');
                        Console.WriteLine(socketresult);
                        socket = socketresult;*/
                    }
                    tcpclnt.Close();
                }

                catch (Exception e)
                {
                    Console.WriteLine("Error..... " + e.StackTrace);
                    return BadRequest("error with engine");
                }
                return Ok();
            }
            catch
            {
                return BadRequest("error");
            }
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate,
X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// depreciated endpoint! ///
        /// <summary>
        /// get current orderbook containing all open orders
        /// </summary>
        /// <param name="symbol">retreieve orders for specified contract</param>
        /// <returns></returns>
        [Route("orderbook")]
        [HttpGet]
        public async Task<IActionResult> getbook(/*[FromHeader]string Authorization,*/ [FromQuery]string symbol)
        {
            try
            {
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                AmazonDynamoDBClient amazonDynamoDBClient = new AmazonDynamoDBClient();
                Table UserTable = Table.LoadTable(amazonDynamoDBClient, "UserDetailsRegistry");
                Document searchdetails = new Document();
                /*
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
                            if (!string.IsNullOrWhiteSpace(doc["DefaultId"]))
                            {
                                if (doc["DefaultSecret"] == secret && doc["DefaultId"] == id)
                                {
                                    Console.WriteLine("successful auth");
                                    searchdetails = doc;
                                    break;
                                }
                            }
                            else
                            {
                                continue;
                            }

                            return BadRequest("Id not found");
                        }
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
                */
                Table orderbooktable;
                if (symbol == "SPXUSD") { orderbooktable = Table.LoadTable(amazonDynamoDBClient, "OrderBook"); }
                else { return BadRequest("bad symbol"); }
                ScanOperationConfig config = new ScanOperationConfig();
                ScanFilter filter = new ScanFilter();
                filter.AddCondition("Price", ScanOperator.IsNotNull);
                config.Filter = filter;
                /*config.AttributesToGet.Add("Price");
                config.AttributesToGet.Add("Volume");
                config.AttributesToGet.Add("Type");*/

                Search searchs = orderbooktable.Scan(config);
                List<Document> orderbook = await searchs.GetRemainingAsync();
                List<ReturnOrderBook> rob = new List<ReturnOrderBook>();

                foreach (Document d in orderbook)
                {
                    rob.Add(new ReturnOrderBook() { Price = d["Price"].ToString(), Volume = d["Volume"].ToString(), Type = d["Type"].ToString() });
                }

                string json = JsonConvert.SerializeObject(rob);
                return Content(json, "application/json");
            }
            catch
            {
                return BadRequest("something went wrong");
            }
        }
    }
}