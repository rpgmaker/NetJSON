using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NetJSON.Tests
{
    [TestClass]
    public class SerializeTests
    {
        public enum MyEnumTest {
            Test1,Test2
        }

        public void CanSerializeMccUserDataObject() {
            var obj = new MccUserData() { arr = new int?[]{10, null, 20} };

            NetJSON.IncludeFields = true;
            NetJSON.GenerateAssembly = true;

            var json = NetJSON.Serialize(obj);
            //var mjson = NetJSON.Deserialize<MccUserData>(json);
            //var r = mjson.arr.Length;
        }

        [TestMethod]
        public void TestEnumInDictionaryObject() {
            var dict = new Dictionary<string, object>();
            dict["Test"] = MyEnumTest.Test2;
            dict["Text"] = "Hello World";

            NetJSON.UseEnumString = true;
            NetJSON.GenerateAssembly = true;

            var json = NetJSON.Serialize(dict);
        }


        [TestMethod]
        public void TestSkippingProperty() {

            NetJSON.GenerateAssembly = true;

            var ss = "{\"aaaaaaaaaa\":\"52\",\"aaaaaURL\":\"x\"}";

            var yy = NetJSON.Deserialize<Foo>(ss);
        }

        [TestMethod]
        public void TestSNDouble() {
            var value = "1.18909";

            var dValue = NetJSON.FastStringToDouble(value);
        }

        public class BaseApiResponse {
            public string @token { get; set; }
            public string @product { get; set; }
            public string @status { get; set; }
            public string @error { get; set; }
        }

        public class TypeHolder {
            public Type Type { get; set; }
        }

        [TestMethod]
        public void TestSerializeException() {
            NetJSON.UseStringOptimization = true;
            var exception = new ExceptionInfoEx {
                Data = new Dictionary<string, string> { { "Test1", "Hello" } },
                ExceptionType = typeof(InvalidCastException),
                HelpLink = "HelloWorld",
                InnerException = new ExceptionInfoEx { HelpLink = "Inner" },
             Message = "Nothing here", Source = "Not found", StackTrace = "I am all here"};

            var json = NetJSON.Serialize(exception);

            var exceptionResult = NetJSON.Deserialize<ExceptionInfoEx>(json);
        }

        public class SimpleObjectWithNull {
            public int Id { get; set; }
            public string EmailAddress { get; set; }
            public string FirstName { get; set; }
            public string Surname { get; set; }
            public int TitleId { get; set; }
            public string Address { get; set; }
        }

        [TestMethod]
        public void TestSimpleObjectSerializationWithNull() {
            var json = "{\"Id\":108591,\"EmailAddress\":\"james.brown@dummy.com\",\"FirstName\":\"James\",\"Surname\":\"Brown\",\"TitleId\":597,\"Address\":null}";
            var simple = NetJSON.Deserialize<SimpleObjectWithNull>(json);
        }

        [TestMethod]
        public void TestSerializeTypeClass() {

            var type = typeof(String);
            var value = NetJSON.Serialize(type);

            var typeType = NetJSON.Deserialize<Type>(value);

            var typeHolder = new TypeHolder { Type = typeof(int) };
            var valueHolder = NetJSON.Serialize(typeHolder);

            var typeHolderType = NetJSON.Deserialize<TypeHolder>(valueHolder);
        }

        [TestMethod]
        public void StringSkippingCauseInfiniteLoop2() {

            NetJSON.UseStringOptimization = true;

            string jsonData = "{ \"token\":\"sFdDNKjLPZJSm0+gvsD1PokoJd3YzbbsClttbWLWz50=\",\"product\":\"productblabla\",\"status\":\"SUCCESS\",\"error\":\"\" }";

            var data = NetJSON.Deserialize<BaseApiResponse>(jsonData);
        }

        public static string ToShortString(short value) {
            return value.ToString();
        }

        public class InvalidJsonStringClass {
            public string ScreenId { get; set; }
            public string StepType { get; set; }
            public string Text { get; set; }
            public string Title { get; set; }
        }

        [TestMethod]
        public void TestInvalidJson() {
            var @string = @"{
    ""ScreenId"": ""Error"",
    ""StepType"": ""Message"",
    ""Text"": ""No se ha encontrado la pagina a la que usted queria ingresar."",
    ""Title"": ""Pagina no encontrada""
}";
            var @string2 = @"{
    ""ScreenId"": ""CRM.IDENTIFICADOR"",
    ""StepType"": ""Screen"",
    ""Title"": ""Identificaci&oacute;n de cliente""
}";
            var data = NetJSON.Deserialize<InvalidJsonStringClass>(@string);
            var data2 = NetJSON.Deserialize<Dictionary<string, string>>(@string2);
        }

        [TestMethod]
        public void SerializeDictionaryWithShortType() {

            //This is not required since short is already handled
            //NetJSON.RegisterTypeSerializer<short>(ToShortString);

            short shortVar = 1;
            int intVar = 1;
            Dictionary<string, object> diccionario = new Dictionary<string, object>();
            diccionario.Add("exampleKey one", shortVar);
            string result = NetJSON.Serialize(diccionario);
            Console.WriteLine(result);
            diccionario.Add("exampleKey two", intVar);
            result = NetJSON.Serialize(diccionario);
        }

        [TestMethod]
        public void TestObjectDeserialize() {
            var value = "\"Test\"";
            var obj = NetJSON.Deserialize<object>(value);
        }

        [TestMethod]
        public void TestSerializePrimitveTypes() {
            var x = 10;
            var s = "Hello World";
            var d = DateTime.Now;

            var xjson = NetJSON.Serialize(x);
            var xx = NetJSON.Deserialize<int>(xjson);

            var sjson = NetJSON.Serialize(s);
            var ss = NetJSON.Deserialize<string>(sjson);
            
            var djson = NetJSON.Serialize(d);
            var dd = NetJSON.Deserialize<DateTime>(djson);

            var ejson = NetJSON.Serialize(SampleEnum.TestEnum1);
            var ee = NetJSON.Deserialize<SampleEnum>(ejson);

            var bjson = NetJSON.Serialize(true);
            var bb = NetJSON.Deserialize<bool>(bjson);
        }

        [TestMethod]
        public void TestJsonFile() {
            NetJSON.CaseSensitive = false;
            using (var file = File.OpenText("json.txt")) {
                var evnts = NetJSON.Deserialize<EvntsRoot>(file.ReadToEnd());
            }
        }

        [TestMethod]
        public void TestSerializeTuple() {

            NetJSON.GenerateAssembly = true;

            var tuple = new Tuple<int, string>(100, "Hello World");

            var json = NetJSON.Serialize(tuple);
            var ttuple = NetJSON.Deserialize<Tuple<int, string>>(json);
        }

        [TestMethod]
        public void TestSerializeComplexTuple() {

            NetJSON.GenerateAssembly = true;

            var tuple = new Tuple<int, DateTime, string,
                          Tuple<double, List<string>>>(1, DateTime.Now, "xisbound",
                    new Tuple<double, List<string>>(45.45, new List<string> { "hi", "man" }));

            var json = NetJSON.Serialize(tuple);
            var ttuple = NetJSON.Deserialize<Tuple<int, DateTime, string, Tuple<double, List<string>>>>(json);
        }

        [TestMethod]
        public void StringSkippingCauseInfiniteLoop() {
            string jsonData = "{\"jsonrpc\":\"2.0\",\"result\":{\"availableToBetBalance\":602.15,\"exposure\":0.0,\"retainedCommission\":0.0,\"exposureLimit\":-10000.0,\"discountRate\":2.0,\"pointsBalance\":1181,\"wallet\":\"UK\"},\"id\":1}";

            var data = NetJSON.Deserialize<JsonRpcResponse<AccountFundsResponse>>(jsonData);
        }

        //[TestMethod]
        public void NestedGraphDoesNotThrow()
        {
            var o = new GetTopWinsResponse()
            {
                TopWins = new List<TopWinDto>()
                {
                    new TopWinDto()
                    {
                        Amount = 1,
                        LandBasedCasino = new TopWinLandBasedCasino()
                        {
                            Location = "Location",
                            MachineName = "Machinename"
                        },
                        Nickname = "Nickname",
                        OnlineCasino = new TopWinOnlineCasino()
                        {
                            GameId = "GameId"
                        },
                        OnlineSports = new TopWinOnlineSports()
                        {
                            AwayTeam = "AwayTeam"
                        },
                        Timestamp = DateTime.Now,
                        Type = TopWinType.LandBasedCasinoWin
                    }
                }
            };
            
            var actual = NetJSON.Serialize(o.GetType(), o);
        }
    }



    public enum ExceptionType
    {
        None,
        Business,
        Security,
        EarlierRequestAlreadyFailed,
        Unknown,
    }

    public class ExceptionInfo
    {
        public ExceptionInfo InnerException { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public string Type { get; set; }

        public string FaultCode { get; set; }

        public ExceptionInfo()
        {
        }

        public ExceptionInfo(Exception exception)
        {
            this.Message = exception.Message;
            this.StackTrace = exception.StackTrace;
            this.Type = exception.GetType().ToString();
            if (exception.InnerException == null)
                return;
            this.InnerException = new ExceptionInfo(exception.InnerException);
        }
    }

    public enum SampleEnum {
        TestEnum1, TestEnum2
    }

    public class ExceptionInfoEx {
        public Type ExceptionType { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public string HelpLink { get; set; }
        public ExceptionInfoEx InnerException { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }

        public static implicit operator ExceptionInfoEx(System.Exception ex) {
            if (ex == null) return null;

            var res = new ExceptionInfoEx {
                ExceptionType = ex.GetType(),
                Data = ex.Data as Dictionary<string, string>,
                HelpLink = ex.HelpLink,
                InnerException = ex.InnerException,
                Message = ex.Message,
                Source = ex.Source,
                StackTrace = ex.StackTrace
            };

            return res;
        }
    }

    public class Response
    {
        public ExceptionInfo Exception { get; set; }

        public ExceptionType ExceptionType { get; set; }

        public bool IsCached { get; set; }

        public Response GetShallowCopy()
        {
            return (Response)this.MemberwiseClone();
        }
    }

    public class GetTopWinsResponse : Response
    {
        public GetTopWinsResponse()
        {
            //TopWins = new List<TopWinDto>();
        }

        public IEnumerable<TopWinDto> TopWins { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var win in TopWins)
                sb.AppendLine(win.ToString());

            return sb.ToString();
        }
    }

    public class Foo {
        public string aaaaaURL { get; set; }

        public string XXXXXXXXXXXXURL { get; set; }
    }

    public class TopWinDto
    {
        public TopWinType Type { get; set; }

        public DateTime Timestamp { get; set; }

        public string Nickname { get; set; }

        public decimal Amount { get; set; }

        public TopWinOnlineCasino OnlineCasino { get; set; }

        public TopWinLandBasedCasino LandBasedCasino { get; set; }

        public TopWinOnlineSports OnlineSports { get; set; }
    }

    public class TopWinOnlineCasino
    {
        public string GameId { get; set; }
    }

    public class TopWinLandBasedCasino
    {
        public string Location { get; set; }

        public string MachineName { get; set; }
    }

    public class SubscriptionInfo {
        public string Name { get; set; }
        public string Topic { get; set; }
        public string ClientToken { get; set; }
        public string AppToken { get; set; }

        public string Key {
            get {
                return String.Concat(Name, Topic);
            }
        }
    }

    public class TopWinOnlineSports
    {
        public DateTime CreationDate { get; set; }

        public string HomeTeam { get; set; }

        public string AwayTeam { get; set; }

        public int Odds { get; set; }

        public int BranchId { get; set; }

        public int LeagueId { get; set; }

        public string YourBet { get; set; }

        public string LeagueName { get; set; }
    }

    public enum TopWinType
    {
        OnlineCasinoWin,
        OnlineSportsWin,
        LandBasedCasinoWin
    }

    public class Group {
        public int id { get; set; }
        public string name { get; set; }
        public List<Group> groups { get; set; }
    }

    public class Root {
        public Group group { get; set; }
    }

    public class Path {
        public string englishName { get; set; }
    }

    public class Event {
        public int id { get; set; }
        public string name { get; set; }
        public string homeName { get; set; }
        public string awayName { get; set; }
        public string start { get; set; }
        public string group { get; set; }
        public string type { get; set; }
        public string boUri { get; set; }
        public List<Path> path { get; set; }
        public string state { get; set; }
    }

    public class Criterion {
        public int id { get; set; }
        public string label { get; set; }
    }

    public class BetOfferType {
        public string name { get; set; }
    }

    public class Outcome {
        public int id { get; set; }
        public string label { get; set; }
        public int odds { get; set; }
        public int line { get; set; }
        public string type { get; set; }
        public int betOfferId { get; set; }
        public string oddsFractional { get; set; }
    }

    public class BetOffer {
        public int id { get; set; }
        public string closed { get; set; }
        public Criterion criterion { get; set; }
        public BetOfferType betOfferType { get; set; }
        public List<Outcome> outcomes { get; set; }
        public int eventId { get; set; }
        //Test remove when NetJSON fixe arrives.
        //public CombinableOutcomes combinableOutcomes { get; set; }
    }

    //Test remove when NetJSON fixe arrives.
    public class CombinableOutcomes {

    }

    public class EvntsRoot {
        public List<BetOffer> betoffers { get; set; }
        public List<Event> events { get; set; }
    }
}
