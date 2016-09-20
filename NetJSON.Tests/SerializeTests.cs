using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJSON.Tests {
    class E
    {
        public int V { get; set; }
    }

    [TestClass]
    public class SerializeTests {
        public enum MyEnumTest {
            Test1, Test2
        }

        public void CanSerializeMccUserDataObject() {
            var obj = new MccUserData() { arr = new int?[] { 10, null, 20 } };

            NetJSON.IncludeFields = true;

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

            var json = NetJSON.Serialize(dict);
        }

        [TestMethod]
        public void TestAutoDetectQuotes() {
            var dict = new Dictionary<string, string>();
            dict["Test"] = "Test2";
            dict["Test2"] = "Test3";

            var list = new List<string>{
                "Test",
                "Test2"
            };

            var str = "Test";
            var settings = new NetJSONSettings { QuoteType = NetJSONQuote.Single };

            var json = NetJSON.Serialize(dict, settings);
            var jsonList = NetJSON.Serialize(list, settings);
            var jsonStr = NetJSON.Serialize(str, settings);

            var jsonWithDouble = json.Replace("'", "\"");
            var jsonListWithDouble = jsonList.Replace("'", "\"");
            var jsonStrWithDouble = jsonStr.Replace("'", "\"");

            var result = NetJSON.Deserialize<Dictionary<string, string>>(jsonWithDouble);
            var result2 = NetJSON.Deserialize<List<string>>(jsonListWithDouble);
            var result3 = NetJSON.Deserialize<string>(jsonStrWithDouble);
        }


        [TestMethod]
        public void TestSkippingProperty() {


            var ss = "{\"aaaaaaaaaa\":\"52\",\"aaaaaURL\":\"x\"}";

            var yy = NetJSON.Deserialize<Foo>(ss);
        }

        [TestMethod]
        public void TestSNDouble() {
            var value = "1.18909";

            var dValue = Internals.SerializerUtilities.FastStringToDouble(value);
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
            var exception = new ExceptionInfoEx {
                Data = new Dictionary<string, string> { { "Test1", "Hello" } },
                ExceptionType = typeof(InvalidCastException),
                HelpLink = "HelloWorld",
                InnerException = new ExceptionInfoEx { HelpLink = "Inner" },
                Message = "Nothing here",
                Source = "Not found",
                StackTrace = "I am all here"
            };

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
        public void TestDictionaryWithColon() {
            var dict = new Dictionary<string, string>();
            dict["Test:Key"] = "Value";
            var json = NetJSON.Serialize(dict);
            var ddict = NetJSON.Deserialize<Dictionary<string, string>>(json);
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

        public class SampleSubstitionClass {
            [NetJSONProperty("blahblah")]
            public string Name { get; set; }
            [NetJSONProperty("foobar")]
            public int ID { get; set; }

            [NetJSONProperty("barfoo")]
            public int Number;
        }

        [TestMethod]
        public void TestNetJSONProperty() {
            NetJSON.IncludeFields = true;

            var sample = new SampleSubstitionClass { ID = 100, Name = "Test Property", Number = 504 };

            var json = NetJSON.Serialize(sample);
            var sData = NetJSON.Deserialize<SampleSubstitionClass>(json);
        }

        public class TestDateTimeFormatting {
            public DateTime DateTimeValue { get; set; }
        }

        [TestMethod]
        public void TestDateTimeFormat() {
            var json = "{\"DateTimeValue\":\"\\/Date(1447003080000+0200)\\/\"}";
            var json2 = "{\"DateTimeValue\":\"2015-11-08T19:18:00+02:00\"}";

            NetJSON.DateFormat = NetJSONDateFormat.Default;
            NetJSON.TimeZoneFormat = NetJSONTimeZoneFormat.Unspecified;
            var obj = NetJSON.Deserialize<TestDateTimeFormatting>(json);
            var sobj = NetJSON.Serialize(obj);

            NetJSON.DateFormat = NetJSONDateFormat.JsonNetISO;
            var obj2 = NetJSON.Deserialize<TestDateTimeFormatting>(json2);
            var sobj2 = NetJSON.Serialize(obj2);
        }

        [TestMethod]
        public void TestDateTimeWithMissingZ()
        {
            var dateString = "{\"DateTimeValue\":\"2015-11-08T19:18:00\"}";
            var date = NetJSON.Deserialize<TestDateTimeFormatting>(dateString);

            var date2 = NetJSON.Serialize(date, 
                new NetJSONSettings { DateFormat = NetJSONDateFormat.ISO,
                 TimeZoneFormat = NetJSONTimeZoneFormat.Utc,
                 QuoteType = NetJSONQuote.Double})
                .Replace("Z", string.Empty).Replace(".0", string.Empty);

            Assert.AreEqual(dateString, date2);
        }

        public class NullableTest
        {
            public int? x { get; set; }
            public int? y { get; set; }
        }

        [TestMethod]
        public void NullableWithDefaultValueSetSerializes()
        {
            var obj = new NullableTest { x = 0, y = null };
            var settings = new NetJSONSettings { SkipDefaultValue = true };
            var json = NetJSON.Serialize(obj, settings);
            Assert.AreEqual("{\"x\":0}", json);
        }

        public class TestJSON {
            public List<Rec> d { get; set; }
            public List<int?> v { get; set; }
            public Dictionary<string, int?> b { get; set; }
        }

        public class Rec {
            public int? val { get; set; }
        }

        [TestMethod]
        public void TestDeserializeNullable() {
            var data = NetJSON.Deserialize<TestJSON>("{\"b\": {\"val1\":1,\"val2\":null,\"val3\":3}, \"v\": [1,2,null,4,null,6], \"d\":[{\"val\":5},{\"val\":null}]}");
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
            var tasks = new List<Task>();

            for (var i = 0; i < 10; i++) {
                tasks.Add(Task.Run(() => {
                    var data = NetJSON.Deserialize<InvalidJsonStringClass>(@string);
                    var data2 = NetJSON.Deserialize<Dictionary<string, string>>(@string2);
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [TestMethod]
        public void SerializeObjectWithQuotes() {
            var obj = new APIQuote { createDate = DateTime.Now, value = "Hello world" };
            var json = NetJSON.Serialize(obj);
            var obj2 = NetJSON.Deserialize<APIQuote>(json);
        }

        [TestMethod]
        public void TestSerializeDateWithMillisecondDefaultFormatLocal() {
            var settings = new NetJSONSettings { DateFormat = NetJSONDateFormat.Default, 
                TimeZoneFormat = NetJSONTimeZoneFormat.Local };
            
            var date = DateTime.UtcNow;
            var djson = NetJSON.Serialize(date, settings);
            var ddate = NetJSON.Deserialize<DateTime>(djson, settings);
            Assert.IsTrue(date == ddate);
        }

        public class APIQuote {
            public DateTime? createDate { get; set; }
            public string value { get; set; }
        }

        [TestMethod]
        public void TestSerializeAlwaysContainsQuotesEvenAfterBeenSerializedInDifferentThreads() {
            var api = new APIQuote { value = "Test" };
            var json = NetJSON.Serialize(api, new NetJSONSettings { QuoteType = NetJSONQuote.Single });
            var json2 = string.Empty;
            Task.Run(() => {
                json2 = NetJSON.Serialize(api, new NetJSONSettings { QuoteType = NetJSONQuote.Single });
            }).Wait();
            Assert.IsTrue(json.Equals(json2), json2);
        }

        [TestMethod]
        public void TestSerializeDictionaryWithComplexDictionaryString() {
            NetJSON.IncludeFields = true;
            Dictionary<string, string> sub1 = new Dictionary<string, string> { { "k1", "v1\"well" }, { "k2", "v2\"alsogood" } };
            var sub1Json = NetJSON.Serialize(sub1);
            Dictionary<string, string> main = new Dictionary<string, string> { 
            { "MK1", sub1Json },
            { "MK2", sub1Json } };

            //At this moment we got in dictionary 2 keys with string values. Every string value is complex and actually is the other serialized Dictionary
            string final = NetJSON.Serialize(main);

            //Trying to get main dictionary back and it fails
            var l1 = NetJSON.Deserialize<Dictionary<string, string>>(final);
            Assert.IsTrue(l1.Count == 2);
        }

        [TestMethod]
        public void SerializeAnonymous()
        {
            var test = new { ID = 100, Name = "Test", Inner = new { ID = 100, N = "ABC" } };
            var json = NetJSON.Serialize(test);
            Assert.IsTrue(json != null);
        }

        private class MyPrivateClass
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public MyPrivateClass Inner { get; set; }
        }

        public enum MyEnumTestValue
        {
            [NetJSONProperty("V_1")]
            V1 = 2,
            [NetJSONProperty("V_2")]
            V2 = 4,
            [NetJSONProperty("V_3")]
            V3 = 5
        }
        public class MyEnumClassTest
        {
            public string Name { get; set; }
            public MyEnumTestValue Value { get; set; }
        }

        [TestMethod]
        public void SerializeEnumValueWithoutCaseUsingAttribute()
        {
            var value = MyEnumTestValue.V1;
            var settings = new NetJSONSettings { UseEnumString = true };

            var json = NetJSON.Serialize(value, settings);
            var value2 = NetJSON.Deserialize<MyEnumTestValue>(json, settings);
            var value3 = NetJSON.Deserialize<MyEnumTestValue>(json.Replace("V_1", "V_2"), settings);

            Assert.IsTrue(value2 == value);
            Assert.IsTrue(value3 == MyEnumTestValue.V2);
        }

        [TestMethod]
        public void SerializeEnumValueUsingAttribute()
        {
            var settings = new NetJSONSettings { UseEnumString = true };
            var obj = new MyEnumClassTest { Name = "Test Enum", Value = MyEnumTestValue.V1 };
            var json = NetJSON.Serialize(obj, settings);

            var obj2 = NetJSON.Deserialize<MyEnumClassTest>(json, settings);

            var obj3 = NetJSON.Deserialize<MyEnumClassTest>(json.Replace("V_1", "V_3"), settings);

            Assert.IsTrue(obj.Value == obj2.Value);
            Assert.IsTrue(obj3.Value == MyEnumTestValue.V3);
        }

        [TestMethod]
        public void TestSerializeEnumFlag()
        {
            var eStr = NetJSON.Serialize(System.IO.FileShare.Read | System.IO.FileShare.Delete, new NetJSONSettings { UseEnumString = true });
            var eInt = NetJSON.Serialize(System.IO.FileShare.Read | System.IO.FileShare.Delete, new NetJSONSettings { UseEnumString = false });

            Assert.IsTrue(eStr == "\"Read, Delete\"");
            Assert.IsTrue(eInt == "5");
        }

        [TestMethod]
        public void When_serializing_anonymous_objects()
        {
            var logEvents = Enumerable.Range(1, 100)
            .Select(n => new LogEvent
            {
                Timestamp = DateTime.UtcNow,
                Level = n % 2 == 0 ? Level.Debug : Level.Trace,
                Entry = n.ToString()
            });

            var anonymousObjects = logEvents
                .Select(x =>
                    new
                    {
                        TimestampEpoch = x.Timestamp,
                        x.Level,
                        Message = x.Entry
                    }
                );

            var json = NetJSON.Serialize(anonymousObjects);
            var resultAsDynamic = NetJSON.Deserialize<dynamic>(json);
            var resultAsObject = NetJSON.Deserialize<object>(json);
            var resultAsProjected = NetJSON.Deserialize<List<Projected>>(json);
        }

        [TestMethod]
        public void TestSerializeDeserializeNonPublicType()
        {
            string s;
            var e = new List<E> { new E { V = 1 }, new E { V = 2 } };
            s = NetJSON.Serialize(e);
            NetJSON.Serialize(NetJSON.Deserialize<List<E>>(s = NetJSON.Serialize(e)));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(s));
        }

        [TestMethod]
        public void SerializeNonPublicType()
        {
            var test = new MyPrivateClass { ID = 100, Name = "Test", Inner = new MyPrivateClass { ID = 200, Name = "Inner" } };
            var json = NetJSON.Serialize(test);
            var data = NetJSON.Deserialize<MyPrivateClass>(json);
            Assert.IsTrue(json != null);
        }

        [TestMethod]
        public void TestSerializeDateUtcNowWithMillisecondDefaultFormatUtc() {
            var settings = new NetJSONSettings { DateFormat = NetJSONDateFormat.Default, TimeZoneFormat = NetJSONTimeZoneFormat.Utc };
            var date = DateTime.UtcNow;
            var djson = NetJSON.Serialize(date, settings);
            var ddate = NetJSON.Deserialize<DateTime>(djson, settings);
            Assert.IsTrue(date == ddate);
        }


        [TestMethod]
        public void TestSerializeDateNowWithMillisecondDefaultFormatUtc() {
            NetJSON.DateFormat = NetJSONDateFormat.Default;
            NetJSON.TimeZoneFormat = NetJSONTimeZoneFormat.Utc;
            var date = DateTime.Now;
            var djson = NetJSON.Serialize(date);
            var ddate = NetJSON.Deserialize<DateTime>(djson);
            Assert.IsTrue(date == ddate);
        }

        [TestMethod]
        public void TestSerializeDateWithMillisecondDefaultFormatUnSpecified() {
            NetJSON.DateFormat = NetJSONDateFormat.Default;
            NetJSON.TimeZoneFormat = NetJSONTimeZoneFormat.Unspecified;
            var date = DateTime.Now;
            var djson = NetJSON.Serialize(date);
            var ddate = NetJSON.Deserialize<DateTime>(djson);
            Assert.IsTrue(date == ddate);
        }

        [TestMethod]
        public void TestSerializeDateWithISOFormatUnSpecified() {
            NetJSON.DateFormat = NetJSONDateFormat.ISO;
            NetJSON.TimeZoneFormat = NetJSONTimeZoneFormat.Unspecified;
            var date = DateTime.Now;
            var djson = NetJSON.Serialize(date);
            var ddate = NetJSON.Deserialize<DateTime>(djson);
            Assert.IsTrue(date == ddate);
        }

        [TestMethod]
        public void TestSerializeDateWithISOFormatLocal() {
            NetJSON.DateFormat = NetJSONDateFormat.ISO;
            NetJSON.TimeZoneFormat = NetJSONTimeZoneFormat.Local;
            var date = DateTime.Now;
            var djson = NetJSON.Serialize(date);
            var ddate = NetJSON.Deserialize<DateTime>(djson);
            Assert.IsTrue(date == ddate);
        }

        [TestMethod]
        public void TestSerializeDateWithISOFormatUTC() {
            NetJSON.DateFormat = NetJSONDateFormat.ISO;
            NetJSON.TimeZoneFormat = NetJSONTimeZoneFormat.Utc;
            var date = new DateTime(2010, 12, 05, 1, 1, 30, 99);
            var djson = NetJSON.Serialize(date);
            var ddate = NetJSON.Deserialize<DateTime>(djson);
            Assert.IsTrue(date == ddate);
        }

        [TestMethod]
        public void TestSerializeDateNowUtcWithISOFormatUTC() {
            var settings = new NetJSONSettings { DateFormat = NetJSONDateFormat.ISO, TimeZoneFormat = NetJSONTimeZoneFormat.Utc };
            
            var date = DateTime.UtcNow;
            var djson = NetJSON.Serialize(date, settings);
            var ddate = NetJSON.Deserialize<DateTime>(djson, settings);
            Assert.IsTrue(date == ddate);
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

        public struct StructWithFields {
            public int x;
            public int y;
        }

        public struct StructWithProperties {
            public int x { get; set; }
            public int y { get; set; }
        }

        [TestMethod]
        public void TestStructWithProperties() {
            var data = new StructWithProperties { x = 10, y = 2 };
            var json = NetJSON.Serialize(data);
            var data2 = NetJSON.Deserialize<StructWithProperties>(json);
            Assert.AreEqual(data.x, data.x);
            Assert.AreEqual(data.y, data.y);
        }

        [TestMethod]
        public void TestStructWithFields() {
            var data = new StructWithFields { x = 10, y = 2 };
            var json = NetJSON.Serialize(data);
            var data2 = NetJSON.Deserialize<StructWithFields>(json);
            Assert.AreEqual(data.x, data.x);
            Assert.AreEqual(data.y, data.y);
        }

        [TestMethod]
        public void TestSerializePrimitiveTypes() {
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


            var tuple = new Tuple<int, string>(100, "Hello World");

            var json = NetJSON.Serialize(tuple);
            var ttuple = NetJSON.Deserialize<Tuple<int, string>>(json);
        }

        [TestMethod]
        public void TestSerializeDeserializeNonPublicSetter() {
            var model = new Person("John", 12);

            var json = NetJSON.Serialize(model);

            var settings = new NetJSONSettings { IncludeTypeInformation = true };
            var deserializedModel = NetJSON.Deserialize<Person>(json, settings);
            Assert.AreEqual("John", deserializedModel.Name);
            Assert.AreEqual(12, deserializedModel.Age);
        }

        [TestMethod]
        public void DtoSerialization() {
            string json;
            List<MyDto> clone;
            int count = 30000;
            var list = new List<MyDto>();
            for (int i = 0; i < count; i++) {
                list.Add(new MyDto { ID = i + 1 });
            }

            json = NetJSON.Serialize(list);
            clone = NetJSON.Deserialize<List<MyDto>>(json);

            Assert.IsTrue(clone.Count == count);
        }

        [TestMethod]
        public void TestSerializeComplexTuple() {

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

        [TestMethod]
        public void SerializeDateTimeOffSet() {
            var settings = new NetJSONSettings { TimeZoneFormat = NetJSONTimeZoneFormat.Local, DateFormat = NetJSONDateFormat.ISO };
            var dateTimeOffset = new DateTimeOffset(DateTime.Now);
            var json = NetJSON.Serialize(dateTimeOffset, settings);

            var dateTimeOffset2 = NetJSON.Deserialize<DateTimeOffset>(json, settings);

            Assert.AreEqual(dateTimeOffset, dateTimeOffset2);
        }

        [TestMethod]
        public void SerializeDateTimeOffSetWithDifferentOffset() {
            var settings = new NetJSONSettings { TimeZoneFormat = NetJSONTimeZoneFormat.Local, DateFormat = NetJSONDateFormat.ISO };

            var now = DateTime.Now;
            var dateTimeOffset = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, new TimeSpan(2, 0, 0));
            

            var json = NetJSON.Serialize(dateTimeOffset, settings);

            var dateTimeOffset2 = NetJSON.Deserialize<DateTimeOffset>(json, settings);

            Assert.AreEqual(dateTimeOffset, dateTimeOffset2);
        }

        [TestMethod]
        public void PrettifyString() {

            var data = new StructWithProperties { x = 10, y = 2 };
            var json = NetJSON.Serialize(data, new NetJSONSettings { Format = NetJSONFormat.Prettify });
            var count = json.Split('\n').Length;

            Assert.IsTrue(count > 1);
        }

        [TestMethod]
        public void TestRootObjectWithInfiniteLoop() {
            //NetJSON.GenerateAssembly = true;
            //var json = File.ReadAllText("netjson_test.txt");
            //var root = NetJSON.Deserialize<Root2>(json);
        }

        [TestMethod]
        public void TestFailedDeserializeOfNullableList() {
            NetJSON.DateFormat = NetJSONDateFormat.ISO;
            NetJSON.SkipDefaultValue = false;
            var listObj = new List<TestJsonClass>()
             {
                new TestJsonClass { time = DateTime.UtcNow.AddYears(1) , id =1 },
                new TestJsonClass { time = DateTime.UtcNow.AddYears(2), id=2 },
                new TestJsonClass { time = DateTime.UtcNow.AddYears(3), id=3 }
            };

            var json = NetJSON.Serialize(listObj);
            //[{"id":0,"time":"1-01-01T00:00:00.0"},{"id":0,"time":"1-01-01T00:00:00.0"},{"id":0,"time":"1-01-01T00:00:00.0"}]

            listObj = NetJSON.Deserialize<List<TestJsonClass>>(json);
        }

        [TestMethod]
        public void TestPossibleInfiniteLoopReproduced() {
            //var obj = new TestNullableNullClass { ID  = 1, Name = "Hello" };
            var json = "{\"ID\": 2, \"Name\": \"Hello world\"}";
            var obj = NetJSON.Deserialize<TestNullableNullClass>(json);
        }

        [TestMethod]
        public void SerializePolyObjects() {
            
            var graph = new Graph { name = "my graph" };
            graph.nodes = new List<Node>();
            graph.nodes.Add(new NodeA { number = 10f });
            graph.nodes.Add(new NodeB { text = "hello" });

            NetJSON.IncludeTypeInformation = true;
            var settings = new NetJSONSettings { IncludeTypeInformation = true };

            var json = NetJSON.Serialize(graph, settings);
            var jgraph = NetJSON.Deserialize<Graph>(json, settings);

            var nodeA = jgraph.nodes[0] as NodeA;
            var nodeB = jgraph.nodes[1] as NodeB;

            Assert.IsTrue(nodeA != null && nodeA.number == 10, json);
            Assert.IsTrue(nodeB != null && nodeB.text == "hello", json);
        }

        [TestMethod]
        public void NestedGraphDoesNotThrow() {
            var o = new GetTopWinsResponse() {
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
            var data = NetJSON.Deserialize<GetTopWinsResponse>(actual);
            Assert.IsTrue(o.TopWins.Count() == data.TopWins.Count());
        }

        [TestMethod]
        public void TestSerializeByteArray() {
            var buffer = new byte[10];
            new Random().NextBytes(buffer);
            var json = NetJSON.Serialize(buffer);
            var data = NetJSON.Deserialize<byte[]>(json);
            Assert.IsTrue(data.Length == buffer.Length);
        }

        [TestMethod]
        public void CanGenerateCamelCaseProperty() {
            var obj = new TopWinOnlineCasino { GameId = "TestGame" };
            var json = NetJSON.Serialize(obj, new NetJSONSettings { CamelCase = true });
            Assert.IsTrue(json.Contains("gameId"));
        }

        [TestMethod]
        public void CannotGenerateCamelCaseProperty() {
            var obj = new TopWinOnlineCasino { GameId = "TestGame" };
            var json = NetJSON.Serialize(obj, new NetJSONSettings { CamelCase = false });
            Assert.IsTrue(json.Contains("GameId"), json);
        }

        [TestMethod]
        public void CanDeserialiseNullableDateTime() {
            var itm = new DateTime(2015, 12, 15);
            var testObj = new NullableTestType<DateTime>(itm);
            var serialised = NetJSON.Serialize(testObj);
            var deserialised = NetJSON.Deserialize<NullableTestType<DateTime>>(serialised);

            Assert.IsNotNull(deserialised);
            Assert.IsNotNull(deserialised.TestItem);
            Assert.AreEqual(testObj.TestItem.Value, itm);
        }

        [TestMethod]
        public void CanDeserialiseNullableTimespan() {
            var itm = new TimeSpan(1500);
            var testObj = new NullableTestType<TimeSpan>(itm);
            var serialised = NetJSON.Serialize(testObj);
            var deserialised = NetJSON.Deserialize<NullableTestType<TimeSpan>>(serialised);

            Assert.IsNotNull(deserialised);
            Assert.IsNotNull(deserialised.TestItem);
            Assert.AreEqual(testObj.TestItem.Value, itm);
        }

        [TestMethod]
        public void CanDeserialiseNullableGuid() {
            var itm = new Guid("10b5a72b-815f-4e64-90bf-cb250840e989");
            var testObj = new NullableTestType<Guid>(itm);
            var serialised = NetJSON.Serialize(testObj);
            var deserialised = NetJSON.Deserialize<NullableTestType<Guid>>(serialised);

            Assert.IsNotNull(deserialised);
            Assert.IsNotNull(deserialised.TestItem);
            Assert.AreEqual(testObj.TestItem.Value, itm);
        }

        [TestMethod]
        public void CaseSensitiveGlossary() {
            var json = @"{
    ""glossary"": {
        ""title"": ""example glossary"",
        ""GlossDiv"": {
            ""title"": ""S"",
            ""GlossList"": {
                ""GlossEntry"": {
                    ""ID"": ""SGML"",
                    ""SortAs"": ""SGML"",
                    ""GlossTerm"": ""Standard Generalized Markup Language"",
                    ""Acronym"": ""SGML"",
                    ""Abbrev"": ""ISO 8879:1986"",
                    ""GlossDef"": {
                        ""para"": ""A meta-markup language, used to create markup languages such as DocBook."",
                        ""GlossSeeAlso"": [""GML"", ""XML""]
                    },
                    ""GlossSee"": ""markup""
                }
            }
        }
    }
}";
            var obj = NetJSON.Deserialize<GlossaryContainer>(json, new NetJSONSettings { CaseSensitive = false });
            Assert.IsNotNull(obj.glossary.glossdiv);
        }

        [TestMethod]
        public void TestPersonClassWithMultipleNonDefaultConstructor() {
            var json = "{ \"name\": \"boss\", \"age\": 2, \"reasonForUnknownAge\": \"he is the boss\" }";
            var data = NetJSON.Deserialize<PersonTest>(json, new NetJSONSettings { CaseSensitive = false });
            Assert.IsTrue(data.Age == 2);
        }

        [TestMethod]
        public void TestSkipDefaultValueWithSetting() {
            var model = new Computer {
                Timestamp = 12345,
                Processes = Enumerable.Range(0, 100)
                    .Select(x => new Process {
                        Id = (uint)x,
                        Name = "P: " + x.ToString(),
                        Description = "This is process " + x.ToString()
                    })
                    .ToArray(),

                OperatingSystems = Enumerable.Range(0, 50)
                    .Select(x => new OperatingSystem {
                        Name = "OS - " + x.ToString(),
                        Version = "0.0.0." + x.ToString(),
                        Price = (decimal)(x * 0.412),
                        Disks = new[]
                        {
                    new Disk
                    {
                        Name = "Disk: " + x.ToString(),
                        Capacity = x * 100
                    },
                    new Disk
                    {
                        Name = "Disk: " + x.ToString(),
                        Capacity = x * 100
                    }
                        }
                    })
                    .ToArray()
            };

            var setting = new NetJSONSettings { SkipDefaultValue = false };

            var modelAsJson = NetJSON.Serialize(model, setting);
            var modelFromJson = NetJSON.Deserialize<Computer>(modelAsJson, setting);
            Assert.AreEqual(model.Processes[0].Id, modelFromJson.Processes[0].Id);
        }

        [TestMethod]
        public void TestIEnumerableClassHolder() {
            var d = new TestEnumerableClass { Data = new List<string> { "a", "b" } };
            var json = NetJSON.Serialize(d);
            var d2 = NetJSON.Deserialize<TestEnumerableClass>(json);
            Assert.IsTrue(d2.Data.Count() == d.Data.Count());
        }
    }

    public struct LogEvent
    {
        public DateTime Timestamp { get; set; }
        public Level Level { get; set; }
        public string Entry { get; set; }
    }

    public enum Level
    {
        Debug,
        Trace
    }

    public class Projected
    {
        public long Timestamp { get; set; }
        public Level Level { get; set; }
        public string Message { get; set; }
    }


    public class TestEnumerableClass {
        public IEnumerable<string> Data { get; set; }
    }

    public class PersonTest {
        public string Name { get; private set; }
        public int? Age { get; private set; }
        public string ReasonForUnknownAge { get; private set; }
        public PersonTest(string name, int age) {
            Age = age;
            Name = name;
        } // age is known
        public PersonTest(string name, string reasonForUnknownAge) {
            Name = name;
            ReasonForUnknownAge = reasonForUnknownAge;
        } // age is unknown, for some reason
    }

    public class TestNullableNullClass {
        public int? ID { get; set; }
        public string Name { get; set; }
    }

    public class NullableTestType<T> where T : struct {
        public Nullable<T> TestItem { get; set; }

        public NullableTestType() { }

        public NullableTestType(T item) {
            TestItem = item;
        }
    }


    public abstract class DtoBase {
        public int ID { get; set; }
    }

    public class MyDto : DtoBase {
        //public int ID { get; set; }
        public string Code { get; set; }
        public string DescriptionShort { get; set; }
        public string DescriptionLong { get; set; }
        public int GroupID { get; set; }
    }

    public class Graph {
        public string name;
        public List<Node> nodes;
    }

    [NetJSONKnownType(typeof(NodeA)), NetJSONKnownType(typeof(NodeB))]
    public class Node {
        //public Vector2 pos;
        public float posx;
        public float posy;
    }

    public class NodeA : Node {
        public float number;
    }

    public class NodeB : Node {
        public string text { get; set; }
    }

    public enum ExceptionType {
        None,
        Business,
        Security,
        EarlierRequestAlreadyFailed,
        Unknown,
    }

    public class ExceptionInfo {
        public ExceptionInfo InnerException { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public string Type { get; set; }

        public string FaultCode { get; set; }

        public ExceptionInfo() {
        }

        public ExceptionInfo(Exception exception) {
            this.Message = exception.Message;
            this.StackTrace = exception.StackTrace;
            this.Type = exception.GetType().ToString();
            if (exception.InnerException == null)
                return;
            this.InnerException = new ExceptionInfo(exception.InnerException);
        }
    }

    public class GlossaryContainer {
        public Glossary glossary { get; set; }

        public GlossaryContainer(string a) { }

        public class Glossary {
            public string title { get; set; }
            public GlossaryDiv glossdiv { get; set; }
        }

        public class GlossaryDiv {
            public string title { get; set; }
            public GlossaryList glosslist { get; set; }
        }

        public class GlossaryList {
            public GlossaryEntry glossentry { get; set; }
        }

        public class GlossaryEntry {
            public string id { get; set; }
            public string sortas { get; set; }
            public string glossterm { get; set; }
            public string acronym { get; set; }
            public string abbrev { get; set; }
            public string glosssee { get; set; }

            public GlossaryDef glossdef { get; set; }
        }

        public class GlossaryDef {
            public string para { get; set; }
            public string[] glossseealso { get; set; }
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

    public class Response {
        public ExceptionInfo Exception { get; set; }

        public ExceptionType ExceptionType { get; set; }

        public bool IsCached { get; set; }

        public Response GetShallowCopy() {
            return (Response)this.MemberwiseClone();
        }
    }

    public class GetTopWinsResponse : Response {
        public GetTopWinsResponse() {
        }

        public IEnumerable<TopWinDto> TopWins { get; set; }

        public override string ToString() {
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

    public class TopWinDto {
        public TopWinType Type { get; set; }

        public DateTime Timestamp { get; set; }

        public string Nickname { get; set; }

        public decimal Amount { get; set; }

        public TopWinOnlineCasino OnlineCasino { get; set; }

        public TopWinLandBasedCasino LandBasedCasino { get; set; }

        public TopWinOnlineSports OnlineSports { get; set; }
    }

    public class TopWinOnlineCasino {
        public string GameId { get; set; }
    }

    public class TopWinLandBasedCasino {
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

    public class TopWinOnlineSports {
        public DateTime CreationDate { get; set; }

        public string HomeTeam { get; set; }

        public string AwayTeam { get; set; }

        public int Odds { get; set; }

        public int BranchId { get; set; }

        public int LeagueId { get; set; }

        public string YourBet { get; set; }

        public string LeagueName { get; set; }
    }

    public sealed class Computer {
        public long Timestamp { get; set; }
        public Process[] Processes { get; set; }
        public OperatingSystem[] OperatingSystems { get; set; }
    }

    public sealed class Process {
        public string Name { get; set; }
        public uint Id { get; set; }
        public string Description { get; set; }
    }

    public sealed class OperatingSystem {
        public string Name { get; set; }
        public string Version { get; set; }
        public decimal Price { get; set; }
        public Disk[] Disks { get; set; }
    }

    public sealed class Disk {
        public string Name { get; set; }
        public long Capacity { get; set; }
    }

    public enum TopWinType {
        OnlineCasinoWin,
        OnlineSportsWin,
        LandBasedCasinoWin
    }

    public sealed class Person {
        public Person(string name, int age) {
            this.Name = name;
            this.Age = age;
        }

        public string Name { get; private set; }
        public int Age { get; private set; }
    }

    public class Group {
        public int id { get; set; }
        public string name { get; set; }
        public List<Group> groups { get; set; }
    }

    public class TestJsonClass {
        public int? id { get; set; }
        public DateTime? time { get; set; }
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

    public class Root2 {
        public Data data { get; set; }
    }

    public class Data {
        public Data2 data { get; set; }
    }

    public class Data2 {
        public Dictionary<String, Sport> sport { get; set; }
    }

    public class Sport {
        public int id { get; set; }
        public string name { get; set; }
        public Dictionary<String, Region> region { get; set; }
    }

    public class Region {
        public int id { get; set; }
        public string name { get; set; }
        public Dictionary<String, Competition> competition { get; set; }
    }

    public class Competition {
        public int id { get; set; }
        public string name { get; set; }
        public Dictionary<String, Game> game { get; set; }
    }

    public class Game {
        public int id { get; set; }
        public int start_ts { get; set; }
        public string team1_name { get; set; }
        public string team2_name { get; set; }
        public int type { get; set; }
        public Info info { get; set; }
        public int markets_count { get; set; }
        public int is_blocked { get; set; }
        public Dictionary<String, Stat> stats { get; set; }
        public bool is_stat_available { get; set; }
    }

    public class Info {
        public string current_game_state { get; set; }
        public string current_game_time { get; set; }
        public string add_minutes { get; set; }
        public string score1 { get; set; }
        public string score2 { get; set; }
        public string shirt1_color { get; set; }
        public string shirt2_color { get; set; }
        public string short1_color { get; set; }
        public string short2_color { get; set; }
    }

    public class Stat {
        public int? team1_value { get; set; }
        public int? team2_value { get; set; }
    }
}
