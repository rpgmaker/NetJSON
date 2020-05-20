using DeepEqual.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NetJSON.Tests {
    class E
    {
        public int V { get; set; }
    }

    [TestClass]
    public class SerializeTests {

        static SerializeTests()
        {
            NetJSON.CanSerialize = CanSerialize;
        }

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

        private static readonly NetJSONSettings Options = new NetJSONSettings
        {
            CamelCase = true
        };

        [TestMethod]
        public void TestSerializeObjectType()
        {
            object model = new List<MentionModel>
            {
                new MentionModel
                {
                    Text = "test",
                    Id = 23232,
                    Title = "test",
                },
                new MentionModel
                {
                    Text = "test",
                    Id = 23232,
                    Title = "test",

                }
            };


            var resultStr = NetJSON.SerializeObject(model, Options);
            Assert.AreNotEqual("[,]", resultStr);
        }

        public class MentionModel
        {
            public long Id { get; set; }

            public string Title { get; set; }

            public string Text { get; set; }
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

        [TestMethod]
        public void NonDefaultNullableValueSerializes()
        {
            var obj = new NullableTest { x = 5 };
            var settings = new NetJSONSettings { SkipDefaultValue = true };
            var json = NetJSON.Serialize(obj, settings);
            Assert.AreEqual("{\"x\":5}", json);
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

        [TestMethod]
        public void TestNetJSONPropertyWithTrackerClassAndFailedJSON()
        {
            string text = "{\n\"Tracker_SortBy\":\"Relevance\",\n\"Tracker_Name\":\"Wind Power Org\",\n\"Profile_Tracker_ID\":1428,\n\"Tracker_ContentType\":\"1\",\n\"Tracker_SearchTerm\":\"Wind Power\",\n\"Tracker_Facets\":[\n{\"Tracker_Facet\":\"{fb8d09e1-5024-419e-9703-598945af8139}\"},\n{\"Tracker_Facet\":\"{90ec93d4-e1bd-4215-acf0-eac1e2fd5f6d}\"}]\n}";
            Tracker t = NetJSON.Deserialize<Tracker>(text);

            Assert.IsTrue(t.FacetCollection.Count > 0);
            Assert.IsTrue(t.ID > 0);
            Assert.IsTrue(t.ContentType != null);
            Assert.IsTrue(t.Name != null);
            Assert.IsTrue(t.SearchTerm != null);
            Assert.IsTrue(t.SortBy != null);
        }

        [TestMethod]
        public void TestNetJSONPropertyTrackerClass()
        {
            var json = @"{  
      ""Tracker_Name"":""xxxx x"",  
      ""Tracker_SearchTerm"":""Wind Power"",    
      ""Tracker_SortBy"":""Relevance""  
    }";

            var json2 = @"{
""Tracker_Name"":""xxxx x x"",
""Tracker_SearchTerm"":""Wind Power"",
""Tracker_SortBy"":""Relevance""
}";
            
            var tracker = NetJSON.Deserialize<Tracker>(json);
            var tracker2 = NetJSON.Deserialize<Tracker>(json2);
            Tracker tracker3 = null;

            using (TextReader reader = new StreamReader(File.OpenRead(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trackerjson.txt"))))
            {
                tracker3 = NetJSON.Deserialize<Tracker>(reader);
            }

            Assert.IsTrue(tracker.Name == "xxxx x", tracker.Name);
            Assert.IsTrue(tracker.SearchTerm == "Wind Power", tracker.SearchTerm);
            Assert.IsTrue(tracker.SortBy == "Relevance", tracker.SortBy);

            Assert.IsTrue(tracker2.Name == "xxxx x x", tracker2.Name);
            Assert.IsTrue(tracker2.SearchTerm == "Wind Power", tracker2.SearchTerm);
            Assert.IsTrue(tracker2.SortBy == "Relevance", tracker2.SortBy);

            Assert.IsTrue(tracker3.Name == "xxxx x", tracker3.Name);
            Assert.IsTrue(tracker3.SearchTerm == "Wind Power", tracker3.SearchTerm);
            Assert.IsTrue(tracker3.SortBy == "Relevance", tracker3.SortBy);
        }

        public struct StructWithFields {
            public int x;
            public int y;
        }

        public struct StructWithProperties {
            public int x { get; set; }
            public int y { get; set; }
            public string Value { get; set; }
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

            var data = new StructWithProperties { x = 10, y = 2, Value = "Data Source=[DataSource,];Initial Catalog=[Database,];User ID=[User,];Password=[Password,];Trusted_Connection=[TrustedConnection,False]" };
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
        [MethodImpl(MethodImplOptions.NoOptimization)]
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
        public void ShouldFailWithBadJSONException()
        {
            Exception ex = null;
            try
            {
                var value = NetJSON.Deserialize<Person>("");
            }
            catch (Exception e) {
                ex = e;
            }

            Assert.IsTrue(ex is NetJSONInvalidJSONException);
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
        public void DeserializeStubbornClass()
        {
            var one = "{\"FileName\":\"973c6d92-819f-4aa1-a0b4-7a645cfea189\",\"Lat\":0,\"Long\":0}";
            var two = "{\"FileName\":\"973c6d92-819f-4aa1-a0b4-7a645cfea189\",\"Lat\":0,\"Long\":0}\n";

            var stubbornOne = NetJSON.Deserialize(typeof(StubbornClass), one);
            var stubbornTwo = NetJSON.Deserialize(typeof(StubbornClass), two);
        }

        [TestMethod]
        public void DeserializeJsonWithMissingQuote()
        {
            var json = @"{
	""document"": ""base64string,
	""documentName"": ""test.pdf"",
	""label1"": ""someLabel"",
	""packageId"": ""7db3eacf-1d2b-4142-9eab-b1bce4630570"",
	""initiator"": ""somerandom @email.com""
}";
            var ex = default(Exception);

            try
            {
                NetJSON.Deserialize<Dictionary<string, string>>(json);
            }
            catch (Exception e)
            {
                ex = e;
            }

            Assert.IsNotNull(ex);
        }

        private class StubbornClass
        {
            public string FileName { get; set; }
            public double Lat { get; set; }
            public double Long { get; set; }
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

        [TestMethod]
        public void TestComplexObjectWithByteArray()
        {
            NetJSON.IncludeTypeInformation = true;
            var obj = new ComplexObject();
            var json = NetJSON.Serialize(obj, new NetJSONSettings { UseEnumString = true });
            var obj2 = NetJSON.Deserialize<ComplexObject>(json, new NetJSONSettings() { UseEnumString = true });

            Assert.IsTrue(obj.Thing6.IsDeepEqual(obj2.Thing6));
        }

        [TestMethod]
        public void TestComplexObjectWithByteArrayWithSerializeType()
        {
            NetJSON.IncludeTypeInformation = true;
            var obj = new ComplexObject();
            var json = NetJSON.Serialize(typeof(ComplexObject), obj);
            var obj2 = NetJSON.Deserialize<ComplexObject>(json);

            Assert.IsTrue(obj.Thing6.IsDeepEqual(obj2.Thing6));
        }

        [TestMethod]
        public void TestEnumHolderWithByteAndShort()
        {
            var settings = new NetJSONSettings { UseEnumString = false };
            var value = new EnumHolder { BEnum = ByteEnum.V2, SEnum = ShortEnum.V2 };
            var json = NetJSON.Serialize(value, settings);

            var bJson = NetJSON.Serialize(ByteEnum.V2, settings);
            var sJson = NetJSON.Serialize(ShortEnum.V2, settings);

            var bValue = NetJSON.Deserialize<ByteEnum>(bJson, settings);
            var sValue = NetJSON.Deserialize<ShortEnum>(sJson, settings);

            var value2 = NetJSON.Deserialize<EnumHolder>(json, settings);

            Assert.IsTrue(value.BEnum == value2.BEnum);
            Assert.IsTrue(value.SEnum == value2.SEnum);
        }

        [TestMethod]
        public void TestSerializationForMicrosoftJavascriptSerializer()
        {
            var json = "{\"CreatorId\":35,\"udtCreationDate\":\"\\/Date(1490945333848)\\/\"}";
            var data = NetJSON.Deserialize<MicrosoftJavascriptSerializerTestData>(json, 
                new NetJSONSettings { DateFormat = NetJSONDateFormat.JavascriptSerializer,
                    TimeZoneFormat = NetJSONTimeZoneFormat.Local });

            Assert.AreEqual(data.CreatorId, 35);
            Assert.AreEqual(data.udtCreationDate.Day, 31);
            Assert.AreEqual(data.udtCreationDate.Month, 3);
            Assert.AreEqual(data.udtCreationDate.Year, 2017);
            Assert.AreEqual(data.udtCreationDate.Hour, 7);
            Assert.AreEqual(data.udtCreationDate.Minute, 28);
            Assert.AreEqual(data.udtCreationDate.Second, 53);
        }

        [TestMethod]
        public void TestEnumFlags()
        {
            var foob = new FooA
            {
                IntVal = 1,
                EnumVal = TestFlags.A | TestFlags.B,
                Type = 2
            };

            var settings = new NetJSONSettings { UseEnumString = true };
            var json = NetJSON.Serialize((FooA)foob, settings);
            var obj = NetJSON.Deserialize<FooA>(json, settings);

            Assert.AreEqual(obj.EnumVal, foob.EnumVal);
        }

        [TestMethod]
        public void TestResultGettingEmptyValueWhenUsingSettings()
        {
            var data = new Result<CustomerResult>
            {
                Data = new CustomerResult { Address = "Test", Id = 1, Name = "Test Name" },
                Limit = 100,
                Offset = 1000,
                TotalResults = 100000
            };

            var json = NetJSON.Serialize(data, new NetJSONSettings {  });

            Assert.IsTrue(!string.IsNullOrWhiteSpace(json));
        }

        [TestMethod]
        public void TestUsingAttributeOfXmlForName()
        {
            var data = new XmlTestClass { Name = "Value" };
            var json = NetJSON.Serialize(data);

            Assert.IsTrue(json.Contains("XmlName"));
        }

        [TestMethod]
        public void TestUsingCustomIgnoreAttribute()
        {
            var data = new TestClassWithIgnoreAttr { ID = 100 };
            var json = NetJSON.Serialize(data);

            Assert.IsTrue(!json.Contains("ID"));
        }

        [TestMethod]
        public void TestUsingTypeAndSettings()
        {
            var userType = typeof(User);
            var settings = new NetJSONSettings { CamelCase = true, CaseSensitive = false };
            User user = new User
            {
                FirstName = "John",
                Id = 23,
                LastName = "Doe",
                Status = UserStatus.Suspended,
                AccountType = AccountType.External
            };

            var json = NetJSON.Serialize(userType, user, settings);
            var user2 = (User)NetJSON.Deserialize(userType, json, settings);

            Assert.AreEqual(user2.FirstName, user.FirstName);
            Assert.AreEqual(user2.Id, user.Id);
            Assert.AreEqual(user2.LastName, user.LastName);
            Assert.AreEqual(user2.Status, user.Status);
            Assert.AreEqual(user2.AccountType, user.AccountType);
        }

        [TestMethod]
        public void TestDictionaryWithEncodedStringParsingWithDictionaryStringObject()
        {
            const string testJsonString = "{\"foo\":\"bar \\\"xyzzy\\\" \"}";
            var deserialisedDictionary = (Dictionary<string, object>)NetJSON.Deserialize<object>(testJsonString);
            var fooValue = (string)deserialisedDictionary["foo"];
            Assert.AreEqual("bar \"xyzzy\" ", fooValue);
        }

        [TestMethod]
        public void TestSerializeAbstractClass()
        {
            PersonAbstract p1 = new PersonX2 { Name = "Bob" };
            var json = NetJSON.Serialize(p1);
            Assert.IsTrue(!string.IsNullOrEmpty(json));
        }

        [TestMethod]
        public void TestSerializeInterfaceType()
        {
            NetJSON.IncludeTypeInformation = true;
            IPerson p1 = new PersonX { Name = "Bob" };
            var json = NetJSON.Serialize(p1);
            Assert.IsTrue(!string.IsNullOrEmpty(json));
        }

        [TestMethod]
        public void TestDeserializeForNullErrorJsonString()
        {
            var json = "{\"success\":true,\"message\":\"\",\"result\":[{\"OrderUuid\":\"47628e98-934f-42dc-9998-eda41174214f\",\"Exchange\":\"BTC-DGB\",\"TimeStamp\":\"2017-09-05T01:30:55.613\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00000419,\"Quantity\":20000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00020950,\"Price\":0.08380000,\"PricePerUnit\":0.00000419000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-05T01:30:55.723\"},{\"OrderUuid\":\"a088f51b-0bb6-48e8-9afa-f08057f60e2b\",\"Exchange\":\"BTC-CVC\",\"TimeStamp\":\"2017-09-04T23:58:11.61\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.00008820,\"Quantity\":1200.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00026459,\"Price\":0.10584299,\"PricePerUnit\":0.00008820000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-04T23:58:33.39\"},{\"OrderUuid\":\"ba00a497-19a7-42e8-bed9-573f12363c5a\",\"Exchange\":\"USDT-ETH\",\"TimeStamp\":\"2017-09-04T18:23:54.803\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":286.38999998,\"Quantity\":6.41404536,\"QuantityRemaining\":0.00000000,\"Commission\":4.59229611,\"Price\":1836.91845050,\"PricePerUnit\":286.38999997000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-04T18:24:05.197\"},{\"OrderUuid\":\"572fad3b-764f-4b23-915e-980ad59023ee\",\"Exchange\":\"BTC-NEO\",\"TimeStamp\":\"2017-09-04T09:13:44.42\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.00514894,\"Quantity\":15.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00019307,\"Price\":0.07723409,\"PricePerUnit\":0.00514893000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-04T09:13:45.297\"},{\"OrderUuid\":\"80ed6fee-a682-47e6-a52b-883ebe228b66\",\"Exchange\":\"BTC-PTOY\",\"TimeStamp\":\"2017-09-03T23:15:11.16\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00008100,\"Quantity\":1000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00020250,\"Price\":0.08100000,\"PricePerUnit\":0.00008100000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-04T08:06:30.217\"},{\"OrderUuid\":\"5c25cbf5-5140-4165-86a4-cac7a62a4b77\",\"Exchange\":\"BTC-CVC\",\"TimeStamp\":\"2017-09-04T04:26:24.96\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00009111,\"Quantity\":1200.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00027333,\"Price\":0.10933200,\"PricePerUnit\":0.00009111000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-04T04:26:25.02\"},{\"OrderUuid\":\"6a60a3fe-2f44-43a8-b915-f841d86584a1\",\"Exchange\":\"BTC-DGB\",\"TimeStamp\":\"2017-09-04T04:25:39.733\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00000534,\"Quantity\":20000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00026699,\"Price\":0.10679998,\"PricePerUnit\":0.00000533000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-04T04:25:39.95\"},{\"OrderUuid\":\"d2409707-3641-42ee-92d1-3c1469fd787c\",\"Exchange\":\"BTC-NEO\",\"TimeStamp\":\"2017-09-01T23:17:20.593\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00600000,\"Quantity\":15.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00022500,\"Price\":0.09000000,\"PricePerUnit\":0.00600000000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-02T01:39:59.697\"},{\"OrderUuid\":\"a92fd25c-259b-428b-a281-9df1994b1cc0\",\"Exchange\":\"BTC-DGB\",\"TimeStamp\":\"2017-09-01T15:51:42.077\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.00000474,\"Quantity\":15000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00017812,\"Price\":0.07125000,\"PricePerUnit\":0.00000475000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-01T15:51:42.187\"},{\"OrderUuid\":\"dd56bde2-5b08-4b13-963e-9b5b64aed0ab\",\"Exchange\":\"BTC-BTS\",\"TimeStamp\":\"2017-09-01T02:49:38.153\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00003260,\"Quantity\":3000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00024375,\"Price\":0.09750000,\"PricePerUnit\":0.00003250000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-09-01T02:49:38.87\"},{\"OrderUuid\":\"01c592e4-e8a7-4a29-a282-06da9eaed847\",\"Exchange\":\"BTC-MCO\",\"TimeStamp\":\"2017-08-31T01:32:14.533\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.00361000,\"Quantity\":25.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00022601,\"Price\":0.09040712,\"PricePerUnit\":0.00361628000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-31T01:32:14.69\"},{\"OrderUuid\":\"257cc430-2f54-4173-9914-6b9af428a5ff\",\"Exchange\":\"BTC-SNT\",\"TimeStamp\":\"2017-08-31T01:27:03.177\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00001045,\"Quantity\":2000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00005224,\"Price\":0.02089999,\"PricePerUnit\":0.00001044000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-31T01:27:03.647\"},{\"OrderUuid\":\"a878d615-2674-4113-94a9-42de4d655ebf\",\"Exchange\":\"BTC-MCO\",\"TimeStamp\":\"2017-08-30T07:25:16.247\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00384998,\"Quantity\":25.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00023750,\"Price\":0.09500000,\"PricePerUnit\":0.00380000000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-30T07:25:16.307\"},{\"OrderUuid\":\"ed06edad-4028-488e-93f9-b3d191505ce5\",\"Exchange\":\"BTC-LTC\",\"TimeStamp\":\"2017-08-29T21:30:17.88\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.01350000,\"Quantity\":30.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00101250,\"Price\":0.40500030,\"PricePerUnit\":0.01350001000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-29T21:30:17.987\"},{\"OrderUuid\":\"6ed65196-fe1e-4beb-abb3-3630073075ab\",\"Exchange\":\"BTC-FCT\",\"TimeStamp\":\"2017-08-28T01:29:52.777\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00677000,\"Quantity\":15.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00025386,\"Price\":0.10154999,\"PricePerUnit\":0.00676999000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-29T13:48:59.93\"},{\"OrderUuid\":\"d1224d72-a014-41c6-a641-631f4505dc25\",\"Exchange\":\"BTC-MTL\",\"TimeStamp\":\"2017-08-29T04:35:07.067\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00222246,\"Quantity\":30.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00016668,\"Price\":0.06667380,\"PricePerUnit\":0.00222246000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-29T04:35:07.177\"},{\"OrderUuid\":\"7670ad26-36cc-4db3-a585-ae0d5a6cf0e9\",\"Exchange\":\"BTC-MCO\",\"TimeStamp\":\"2017-08-29T01:57:13.33\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.00597699,\"Quantity\":25.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00037356,\"Price\":0.14942475,\"PricePerUnit\":0.00597699000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-29T01:57:14.723\"},{\"OrderUuid\":\"488896b7-9a7f-4619-bf74-32a4f6e50e56\",\"Exchange\":\"BTC-DGB\",\"TimeStamp\":\"2017-08-28T23:26:07.003\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00000368,\"Quantity\":15000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00013762,\"Price\":0.05504999,\"PricePerUnit\":0.00000366000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-28T23:26:07.19\"},{\"OrderUuid\":\"869e97ca-2c10-44f8-9d17-b26dc0d70dac\",\"Exchange\":\"BTC-SNT\",\"TimeStamp\":\"2017-08-28T02:00:21.013\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00001169,\"Quantity\":5000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00014612,\"Price\":0.05845000,\"PricePerUnit\":0.00001169000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-28T02:00:21.527\"},{\"OrderUuid\":\"f2a1299b-c486-4548-a64b-7350b963d523\",\"Exchange\":\"BTC-NXT\",\"TimeStamp\":\"2017-08-28T01:24:20.477\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00002605,\"Quantity\":3200.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00020840,\"Price\":0.08336000,\"PricePerUnit\":0.00002605000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-28T01:28:04.66\"},{\"OrderUuid\":\"bca820a3-7aaf-4f28-9808-7c9b59a135e2\",\"Exchange\":\"BTC-GAME\",\"TimeStamp\":\"2017-08-28T01:09:55.843\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00053247,\"Quantity\":150.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00019967,\"Price\":0.07987049,\"PricePerUnit\":0.00053246000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-28T01:21:47.413\"},{\"OrderUuid\":\"5054479b-4ea5-4591-9355-84cf6bb1d5dd\",\"Exchange\":\"BTC-MCO\",\"TimeStamp\":\"2017-08-28T01:11:30.16\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00366017,\"Quantity\":25.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00022874,\"Price\":0.09150424,\"PricePerUnit\":0.00366016000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-28T01:13:36.31\"},{\"OrderUuid\":\"3b60e7f5-2082-447b-9c5e-9f402a06332c\",\"Exchange\":\"BTC-BAT\",\"TimeStamp\":\"2017-08-28T01:12:56.447\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00004913,\"Quantity\":1800.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00022108,\"Price\":0.08843400,\"PricePerUnit\":0.00004913000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-28T01:12:56.51\"},{\"OrderUuid\":\"e3419c0f-e252-4b61-a92b-398032ced5f3\",\"Exchange\":\"BTC-XRP\",\"TimeStamp\":\"2017-08-28T01:07:16.203\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00004618,\"Quantity\":1700.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00019626,\"Price\":0.07850600,\"PricePerUnit\":0.00004618000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-28T01:07:16.423\"},{\"OrderUuid\":\"62dac844-8f5f-48f6-8b47-4384cf584ac8\",\"Exchange\":\"BTC-LTC\",\"TimeStamp\":\"2017-08-28T01:03:58.97\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.01427100,\"Quantity\":50.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00178462,\"Price\":0.71385946,\"PricePerUnit\":0.01427718000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-28T01:03:59.267\"},{\"OrderUuid\":\"2a3b4183-8a3e-4b90-94eb-d9d74104b199\",\"Exchange\":\"BTC-VTC\",\"TimeStamp\":\"2017-08-27T05:47:09.587\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.00021552,\"Quantity\":1000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00053879,\"Price\":0.21551998,\"PricePerUnit\":0.00021551000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-27T05:48:06.34\"},{\"OrderUuid\":\"21a80737-ee52-4175-8184-5eb8260bd977\",\"Exchange\":\"BTC-MCO\",\"TimeStamp\":\"2017-08-27T00:21:11.477\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.00384073,\"Quantity\":30.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00028875,\"Price\":0.11550000,\"PricePerUnit\":0.00385000000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-27T00:21:11.65\"},{\"OrderUuid\":\"708f5cfe-94ab-4936-88a9-0a9e82619b86\",\"Exchange\":\"BTC-MCO\",\"TimeStamp\":\"2017-08-26T01:30:37.887\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00243500,\"Quantity\":30.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00018262,\"Price\":0.07304999,\"PricePerUnit\":0.00243499000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-26T01:46:34.627\"},{\"OrderUuid\":\"9cd60d42-e432-46b8-ba20-fbfc710f95ce\",\"Exchange\":\"BTC-BCC\",\"TimeStamp\":\"2017-08-19T16:22:05.547\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.20500000,\"Quantity\":2.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00102498,\"Price\":0.40999999,\"PricePerUnit\":0.20499999000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-19T16:22:06.797\"},{\"OrderUuid\":\"63297fe5-d07b-40ce-97a2-f960c0b107d3\",\"Exchange\":\"BTC-BCC\",\"TimeStamp\":\"2017-08-18T01:57:00.053\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":0.11124700,\"Quantity\":3.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00083435,\"Price\":0.33374100,\"PricePerUnit\":0.11124700000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-18T01:57:00.837\"},{\"OrderUuid\":\"bf1b3b89-2fce-49c0-b353-0c89bea18c24\",\"Exchange\":\"BTC-VTC\",\"TimeStamp\":\"2017-08-16T22:57:19.757\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00010100,\"Quantity\":1000.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00025249,\"Price\":0.10100000,\"PricePerUnit\":0.00010100000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-17T00:05:27.51\"},{\"OrderUuid\":\"c6b11b9b-495e-4ac8-8a94-5b73d6e233bc\",\"Exchange\":\"BTC-FCT\",\"TimeStamp\":\"2017-08-16T21:54:31.163\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.00435940,\"Quantity\":13.44236162,\"QuantityRemaining\":0.00000000,\"Commission\":0.00014650,\"Price\":0.05860063,\"PricePerUnit\":0.00435939000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-16T21:57:45.983\"},{\"OrderUuid\":\"daab0461-8061-43e1-af48-92dcf38bda3f\",\"Exchange\":\"USDT-BTC\",\"TimeStamp\":\"2017-08-16T21:46:22.13\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":4357.00000000,\"Quantity\":0.16000000,\"QuantityRemaining\":0.00000000,\"Commission\":1.74280000,\"Price\":697.12000000,\"PricePerUnit\":4357.00000000000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-16T21:46:40.347\"},{\"OrderUuid\":\"ed50ef1f-79a4-4474-966d-756f668164cd\",\"Exchange\":\"USDT-BTC\",\"TimeStamp\":\"2017-08-08T17:23:48.917\",\"OrderType\":\"LIMIT_SELL\",\"Limit\":3363.00000001,\"Quantity\":0.30055068,\"QuantityRemaining\":0.00000000,\"Commission\":2.52687984,\"Price\":1010.75193684,\"PricePerUnit\":3363.00000000000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-08T17:23:50.433\"},{\"OrderUuid\":\"e7d95fcb-f0d4-433a-80d2-823c24a00e93\",\"Exchange\":\"BTC-GBYTE\",\"TimeStamp\":\"2017-08-07T18:33:41.96\",\"OrderType\":\"LIMIT_BUY\",\"Limit\":0.12000011,\"Quantity\":1.00000000,\"QuantityRemaining\":0.00000000,\"Commission\":0.00028301,\"Price\":0.11320604,\"PricePerUnit\":0.11320604000000000000,\"IsConditional\":false,\"Condition\":\"NONE\",\"ConditionTarget\":null,\"ImmediateOrCancel\":false,\"Closed\":\"2017-08-07T18:33:42.083\"}]}";
            API_ImportResult result = NetJSON.Deserialize<API_ImportResult>(json);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.result.Count > 0);
        }

        [TestMethod]
        public void TestDeserializeValueWithDoubleQuotes()
        {
            var settings = new NetJSONSettings
            {
                CaseSensitive = false
            };

            // Actual string { Val : "\"sampleValue\""} before escape characters
            var stringToDeserialize = "{ \"Val\" : \"\\\"sampleValue\\\"\"}";
            
            var result = NetJSON.Deserialize<Test>(stringToDeserialize, settings);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Val);
        }

        [TestMethod]
        public void TestNotThrowingInvalidJSONForPrimitiveTypes()
        {
            var value = NetJSON.Deserialize<string>("\"\"abc\"");

            Assert.AreEqual("\"abc", value);
        }

        [TestMethod]
        public void TestNotThrowingInvalidJSONForNullPrimitiveTypes()
        {
            var value = NetJSON.Deserialize<string>(default(string));

            Assert.AreEqual(null, value);
        }

        [TestMethod]
        public void TestSimpleStruct()
        {
            var settings = new NetJSONSettings();
            var data = new SimpleObjectStruct() { ID = 10, Name = "Test", Value = "Tester" };
            var json = NetJSON.Serialize(data, settings);
            var data2 = NetJSON.Deserialize<SimpleObjectStruct>(json, settings);

            Assert.AreEqual(data.ID, data2.ID);
            Assert.AreEqual(data.Name, data2.Name);
            Assert.AreEqual(data.Value, data2.Value);
        }

        [TestMethod]
        public void CanDeserializeLargeNumbers()
        {
            var test = NetJSON.Deserialize<Dictionary<string, object>>("{\"test\":9999999999}");
            Assert.AreEqual(test["test"], 9999999999);
        }

        [TestMethod]
        public void CanSerializeAndDeserializedEscapeStringInDictionary()
        {
            var testDictionary = new Dictionary<string, object>();
            testDictionary["Path"] = @"\\fabcde\pabcde\abcde\abcde.txt";

            var json = NetJSON.Serialize(testDictionary);
            var data = NetJSON.Deserialize<Dictionary<string, object>>(json);
            Assert.AreEqual(testDictionary["Path"], data["Path"]);
        }

        [TestMethod]
        public void CanDeserializeKeyAndValueProperly()
        {
            var xy = new A();
            xy.Details.Add(666, null);

            var json = NetJSON.Serialize(xy);
            var obj = NetJSON.Deserialize<A>(json);

            Assert.IsTrue(xy.Details.ContainsKey(666));
        }

        [TestMethod]
        public void CanDeserilizeDictionaryKeyAndValue()
        {
            var dict = new Dictionary<int, B>();
            dict.Add(666, new B());
            var json = NetJSON.Serialize(dict);
            var obj = NetJSON.Deserialize<Dictionary<int, B>>(json);

            Assert.IsTrue(obj.ContainsKey(666));
        }

        [TestMethod]
        public void CanDeserializeObjectWithDefaultValueForBoolean()
        {
            var usr = NetJSON.Deserialize<YadroUser>(File.ReadAllText("netjsonObj.txt"), new NetJSONSettings { SkipDefaultValue = false });
            Assert.IsFalse(usr.Enabled);
        }

        [TestMethod]
        public void CanDeserializeObjectWithDefaultValueOfFalseBoolean()
        {
            var json = "{\"Enabled\": false}";
            var data = NetJSON.Deserialize<EnabledClass>(json);
            Assert.IsFalse(data.Enabled);
        }

        private static readonly NetJSONSettings Settings = new NetJSONSettings { DateFormat = NetJSONDateFormat.ISO, TimeZoneFormat = NetJSONTimeZoneFormat.Utc };

        [TestMethod]
        public void HandlesNullable()
        {
            var nullable = new NullableEntity { Id = Guid.NewGuid(), Value = new ValueObject { Value = "Test" } };
            var serialised = NetJSON.Serialize(nullable, Settings);
            var deserialised = NetJSON.Deserialize<NullableEntity>(serialised, Settings);
            Assert.AreEqual(nullable.Id, deserialised.Id);
            Assert.AreEqual(nullable.Value.Value, deserialised.Value.Value);

            nullable = new NullableEntity();
            serialised = NetJSON.Serialize(nullable, Settings);
            deserialised = NetJSON.Deserialize<NullableEntity>(serialised, Settings);
            Assert.IsFalse(deserialised.Id.HasValue);
        }

        [TestMethod]
        public void HandlesReadOnlyDictionary()
        {
            var entity = new EntityWithReadOnlyDictionary { Map = new Dictionary<string, string> { { "One", "Eno" }, { "Two", "Owt" }, { "Three", "Eerht" } } };
            var serialised = NetJSON.Serialize(entity, Settings);
            var deserialised = NetJSON.Deserialize<EntityWithReadOnlyDictionary>(serialised, Settings);

            Assert.AreNotSame(entity.Map, deserialised.Map);
            Assert.AreEqual(entity.Map.Count, deserialised.Map.Count);
            foreach (var item in entity.Map)
            {
                Assert.IsTrue(deserialised.Map.ContainsKey(item.Key));
                Assert.AreEqual(item.Value, deserialised.Map[item.Key]);
            }
        }

        [TestMethod]
        public void HandlesReadOnlyCollection()
        {
            var entity = new Entity { Items = new[] { "One", "Two", "Three" } };
            var serialised = NetJSON.Serialize(entity, Settings);
            var deserialised = NetJSON.Deserialize<Entity>(serialised, Settings);

            Assert.AreEqual(entity.Items.Count(), deserialised.Items.Count());
            foreach (var item in entity.Items)
                Assert.IsTrue(deserialised.Items.Contains(item));
        }

        [TestMethod]
        public void HandlesReadOnlyList()
        {
            var entity = new EntityWithReadOnlyList { Strings = new List<string> { "Test", "Test2" }  };
            var serialised = NetJSON.Serialize(entity, Settings);
            var deserialised = NetJSON.Deserialize<EntityWithReadOnlyList>(serialised, Settings);

            Assert.AreEqual(2, deserialised.Strings.Count);
            Assert.AreEqual("Test", deserialised.Strings[0]);
            Assert.AreEqual("Test2", deserialised.Strings[1]);
        }

        private static bool CanSerialize(MemberInfo memberInfo)
        {
            var attr = memberInfo.GetCustomAttribute<TestIgnoreAttribute>();
            if(attr != null)
            {
                return false;
            }

            return true;
        }

        private static readonly Random Random = new Random();

        [TestMethod]
        public void EventStructTest()
        {
            var e = new EventStruct(Guid.NewGuid(), new PayloadStruct(Guid.NewGuid().ToString("n")), Random.Next(), DateTimeOffset.UtcNow);
            var s = NetJSON.Serialize(e, Settings);
            var d = NetJSON.Deserialize<EventStruct>(s, Settings);

            Assert.AreEqual(e.Id, d.Id);
            Assert.AreEqual(e.Payload.Value, d.Payload.Value);
            Assert.AreEqual(e.Version, d.Version);
            Assert.AreEqual(e.Created, d.Created);
        }

        [TestMethod]
        public void EventStructWithPrivateSettersTest()
        {
            var e = new EventStructWithPrivateSetters(Guid.NewGuid(), new PayloadStructWithPrivateSetter(Guid.NewGuid().ToString("n")), Random.Next(), DateTimeOffset.UtcNow);
            var s = NetJSON.Serialize(e, Settings);
            var d = NetJSON.Deserialize<EventStructWithPrivateSetters>(s, Settings);

            Assert.AreEqual(e.Id, d.Id);
            Assert.AreEqual(e.Payload.Value, d.Payload.Value);
            Assert.AreEqual(e.Version, d.Version);
            Assert.AreEqual(e.Created, d.Created);
        }

        [TestMethod]
        public void EventStructWithReadOnlyBackingFieldsTest()
        {
            var e = new EventStructWithReadOnlyBackingFields(Guid.NewGuid(), new PayloadStructWithReadOnlyBackingField(Guid.NewGuid().ToString("n")), Random.Next(), DateTimeOffset.UtcNow);
            var s = NetJSON.Serialize(e, Settings);
            var d = NetJSON.Deserialize<EventStructWithReadOnlyBackingFields>(s, Settings);

            Assert.AreEqual(e.Id, d.Id);
            Assert.AreEqual(e.Payload.Value, d.Payload.Value);
            Assert.AreEqual(e.Version, d.Version);
            Assert.AreEqual(e.Created, d.Created);
        }

        [TestMethod]
        public void EventClassTest()
        {
            var e = new EventClass(Guid.NewGuid(), new PayloadClass(Guid.NewGuid().ToString("n")), Random.Next(), DateTimeOffset.UtcNow);
            var s = NetJSON.Serialize(e, Settings);
            var d = NetJSON.Deserialize<EventClass>(s, Settings);

            Assert.AreEqual(e.Id, d.Id);
            Assert.AreEqual(e.Payload.Value, d.Payload.Value);
            Assert.AreEqual(e.Version, d.Version);
            Assert.AreEqual(e.Created, d.Created);
        }

        [TestMethod]
        public void EventClassWithPrivateSettersTest()
        {
            var e = new EventClassWithPrivateSetters(Guid.NewGuid(), new PayloadClassWithPrivateSetter(Guid.NewGuid().ToString("n")), Random.Next(), DateTimeOffset.UtcNow);
            var s = NetJSON.Serialize(e, Settings);
            var d = NetJSON.Deserialize<EventClassWithPrivateSetters>(s, Settings);

            Assert.AreEqual(e.Id, d.Id);
            Assert.AreEqual(e.Payload.Value, d.Payload.Value);
            Assert.AreEqual(e.Version, d.Version);
            Assert.AreEqual(e.Created, d.Created);
        }

        [TestMethod]
        public void EventClassWithReadOnlyBackingFieldsTest()
        {
            var e = new EventClassWithReadOnlyBackingFields(Guid.NewGuid(), new PayloadClassWithWithReadOnlyBackingField(Guid.NewGuid().ToString("n")), Random.Next(), DateTimeOffset.UtcNow);
            var s = NetJSON.Serialize(e, Settings);
            var d = NetJSON.Deserialize<EventClassWithReadOnlyBackingFields>(s, Settings);

            Assert.AreEqual(e.Id, d.Id);
            Assert.AreEqual(e.Payload.Value, d.Payload.Value);
            Assert.AreEqual(e.Version, d.Version);
            Assert.AreEqual(e.Created, d.Created);
        }

        [TestMethod]
        public void HandlesGuids()
        {
            var value = Guid.NewGuid();
            var serialised = NetJSON.Serialize(value, Settings);
            var deserialised = NetJSON.Deserialize<Guid>(serialised, Settings);
            Assert.AreEqual(value, deserialised);
            var values = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            serialised = NetJSON.Serialize(values, Settings);
            var deserialisedArray = NetJSON.Deserialize<List<Guid>>(serialised, Settings); // Fails
        }

        [TestMethod]
        public void HandlesDateTimeOffsetDictionaryKey()
        {
            var value = DateTimeOffset.UtcNow;
            var s = NetJSON.Serialize(value, Settings); // "2018-11-28T10:41:03.987489Z"
            Assert.AreEqual(value, NetJSON.Deserialize<DateTimeOffset>(s));
            var map = new Dictionary<DateTimeOffset, int> { { value, Random.Next() } };
            var serialised = NetJSON.Serialize(map, Settings); // {"28/11/2018 10:41:29 +00:00":266037427}
            var deserialised = NetJSON.Deserialize<Dictionary<DateTimeOffset, int>>(serialised, Settings);

            Assert.AreEqual(value, deserialised.Keys.Single()); // Fails
            Assert.AreEqual(map[value], deserialised[value]);
        }

        [TestMethod]
        public void HandlesDateTimeDictionaryKey()
        {
            var value = DateTime.UtcNow;
            var s = NetJSON.Serialize(value, Settings); // "2018-11-28T10:35:50.9314230Z"
            Assert.AreEqual(value, NetJSON.Deserialize<DateTime>(s));

            var map = new Dictionary<DateTime, int> { { value, Random.Next() } };
            var serialised = NetJSON.Serialize(map, Settings); // {"28/11/2018 10:37:26":871282158}
            var deserialised = NetJSON.Deserialize<Dictionary<DateTime, int>>(serialised, Settings);

            Assert.AreEqual(value, deserialised.Keys.Single()); // Fails
            Assert.AreEqual(map[value], deserialised[value]);
        }

        [TestMethod]
        public void ExpandoSerializationWithStringDoubleSlash()
        {
            var json = @"[{""StringValue"":""C:\\ProgramData\\""}]";
            var dict = NetJSON.Deserialize<List<Dictionary<string, object>>>(json);
            var expando = NetJSON.Deserialize<List<ExpandoObject>>(json);
            Assert.AreEqual(dict[0]["StringValue"], "C:\\ProgramData\\");
        }

        [TestMethod]
        public unsafe void SerializeWithCustomType()
        {
            var model = new TestClassForCustomSerialization { ID = 100, Custom = new UserDefinedCustomClass { Name = "Test" } };

            NetJSON.RegisterCustomTypeSerializer<UserDefinedCustomClass>(UserDefinedCustomClass.Serialize);
            NetJSON.RegisterCustomTypeDeserializer(UserDefinedCustomClass.Deserialize);

            var json = NetJSON.Serialize(model);
            var model2 = NetJSON.Deserialize<TestClassForCustomSerialization>(json);

            Assert.AreEqual(model.ID, model2.ID);
            Assert.AreEqual(model.Custom.Name, model2.Custom.Name);
        }


        [TestMethod]
        public void SerializeDeserializeStrings()
        {
            var netJsonSettings = new NetJSONSettings
            {
                CaseSensitive = true,
                DateFormat = NetJSONDateFormat.ISO,
                IncludeTypeInformation = true,
                UseEnumString = true,
                UseStringOptimization = true
            };
            var myString = "MyString";

            var serialized = NetJSON.Serialize(myString, netJsonSettings);
            var result = NetJSON.Deserialize<string>(serialized, netJsonSettings);

            Assert.AreEqual(myString, result);
        }

        [TestMethod]
        public void ShouldThrowExceptionForInvalidType()
        {
            //should not hangs
            var badJson = "{  \"Id\": 31,  \"SubStruct\":  1,  \"SomeString\": \"My test string\"}";
            MySuperStruct myStruct = null;
            Exception ex = null;
            try
            {
                myStruct = NetJSON.Deserialize<MySuperStruct>(badJson);
            }
            catch (Exception e)
            {
                ex = e;
            }

            Assert.IsNotNull(ex);
            Assert.IsInstanceOfType(ex, typeof(NetJSONTypeMismatchException));
            Assert.IsNull(myStruct);
        }

        [TestMethod]
        public void ShouldNotFailWhenUsingUlongEnum()
        {
            var obj = new ULongClass { LongEnum = ULongEnum.A };
            var json = NetJSON.Serialize(obj);
            var data = NetJSON.Deserialize<ULongClass>(json);

            Assert.AreEqual((ulong)data.LongEnum, (ulong)obj.LongEnum);
        }

        [TestMethod]
        public void ShouldNotFailWhenUlongFlagEnum()
        {
            var obj = new ULongClass() { LongEnum = ULongEnum.ULongEnumValueA | ULongEnum.ULongEnumValueB };
            var json = NetJSON.Serialize(obj);
            var dto = NetJSON.Deserialize<ULongClass>(json);
            var ulongValue = (ulong)dto.LongEnum;

            Assert.AreEqual((ulong)dto.LongEnum, (ulong)obj.LongEnum);
        }


        [TestMethod]
        public void ShouldSerializeIntWithDefault()
        {
            var settings = new NetJSONSettings { SkipDefaultValue = false };
            var json = "{\"Id\":0}";
            var obj2 = NetJSON.Deserialize<IntWithDefault>(json, settings);

            Assert.AreEqual(0, obj2.Id);
        }

        [TestMethod]
        public void ShouldSerializeNullableIntWithDefault()
        {
            var settings = new NetJSONSettings { SkipDefaultValue = false };
            var json = NetJSON.Serialize(new IntNullableDefault { Id = null }, settings);
            var obj2 = NetJSON.Deserialize<IntNullableDefault>(json, settings);

            Assert.IsNull(obj2.Id);
        }

        [TestMethod]
        public void ShouldSerializeNullableIntWithValue()
        {
            var settings = new NetJSONSettings { SkipDefaultValue = false };
            var json = NetJSON.Serialize(new IntNullableDefault { Id = 0 }, settings);
            var obj2 = NetJSON.Deserialize<IntNullableDefault>(json, settings);

            Assert.IsTrue(obj2.Id == 0);
        }

        [TestMethod]
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CanSerializeSimpleClassWithInterface()
        {
            NetJSON.IncludeTypeInformation = true;
            var jsonSettingsB = new NetJSONSettings() { DateFormat = NetJSONDateFormat.ISO, SkipDefaultValue = false, UseEnumString = true, };
            var sc = new SimpleClass();
            var isc = (ISimpleClass)sc;
            var scs = NetJSON.Serialize(typeof(SimpleClass), sc, jsonSettingsB);
            var iscs = NetJSON.Serialize(typeof(ISimpleClass), isc, jsonSettingsB);
            var scsd = NetJSON.Deserialize<ISimpleClass>(iscs, jsonSettingsB) as ISimpleClass;

            Assert.AreEqual(scsd.SimpleClassProp, sc.SimpleClassProp);
            Assert.AreEqual(scsd.SimpleClassBaseProp, sc.SimpleClassBaseProp);
        }

        [TestMethod]
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void CanSerializeCollectionSimpleWithInterface()
        {
            NetJSON.IncludeTypeInformation = true;
            var jsonSettings = new NetJSONSettings() { DateFormat = NetJSONDateFormat.ISO, SkipDefaultValue = false, UseEnumString = true };
            var isc = (ISimpleClassBase)new SimpleClass();
            var iscEnumerable = new List<ISimpleClassBase>() { new SimpleClass() };
            var iscs = NetJSON.SerializeObject(isc, jsonSettings);
            var iscsE = NetJSON.SerializeObject(iscEnumerable, jsonSettings);
            var discsE = NetJSON.Deserialize<List<ISimpleClassBase>>(iscsE, jsonSettings);
            Assert.AreEqual(1, discsE.Count);
            Assert.AreEqual((discsE[0] as SimpleClass).SimpleClassProp, (isc as SimpleClass).SimpleClassProp);
            Assert.AreEqual(discsE[0].SimpleClassBaseProp, isc.SimpleClassBaseProp);
        }

        [TestMethod]
        public void ShouldNotFailWhenUsingNegativeIntEnumWithEnumString()
        {
            var settings = new NetJSONSettings { UseEnumString = true };
            var obj = new IntClass { LongEnum = IntEnum.ID2 };
            var json = NetJSON.Serialize(obj, settings);
            var data = NetJSON.Deserialize<IntClass>(json, settings);

            Assert.AreEqual((int)data.LongEnum, (int)obj.LongEnum);
        }

        [TestMethod]
        public void ShouldNotFailWhenUsingNegativeIntEnumWithValue()
        {
            var settings = new NetJSONSettings { UseEnumString = false };
            var obj = new IntClass { LongEnum = IntEnum.ID2 };
            var json = NetJSON.Serialize(obj, settings);
            var data = NetJSON.Deserialize<IntClass>(json, settings);

            Assert.AreEqual((int)data.LongEnum, (int)obj.LongEnum);
        }


        [TestMethod]
        public void ShouldNotFailFloatConvert()
        {
            var testFloat = new TestFloatClass
            {
                IntValue = 10,
                FloatValue = 10.5f,
                StringValue = "10"
            };

            var json = NetJSON.Serialize(testFloat);
            var testFloatData = NetJSON.Deserialize<TestFloatClass>(json);

            Assert.AreEqual(10.5f, testFloatData.FloatValue);
        }

        [TestMethod]
        public void CanDeserializeLargeDictionary()
        {
            string str = "{\"SizedImages\":{\"$type\":\"System.Collections.Generic.List1[[y.Go.X.Contracts.Denormalization.TileSizedImage, y.Go.X.Contracts]], mscorlib\",\"$values\":[{\"Role\":\"photo\",\"Description\":\"ORF / ©Twentieth Century Fox Film Corporation. All rights reserved.\",\"IsMain\":true,\"Large\":{\"Url\":\"http://aaa.com/images/828r_or1_191021_1535_7570e136_malcolm_mittendrin.jpg\",\"Width\":828,\"Height\":465},\"Medium\":{\"Url\":\"http://aaa.com/images/414r_or1_191021_1535_7570e136_malcolm_mittendrin.jpg\",\"Width\":414,\"Height\":232},\"Small\":{\"Url\":\"http://aaa.com/images/200r_or1_191021_1535_7570e136_malcolm_mittendrin.jpg\",\"Width\":200,\"Height\":112}}]},\"SubTitle\":\"Malcolm, der Held\",\"EpisodeId\":\"41270363\",\"EpisodeNum\":\"1\",\"SeriesId\":\"1129\",\"SeasonNum\":null,\"ChannelId\":1,\"ChannelName\":\"ORFeins HD\",\"ChannelIcon\":\"https://aaa/1.png\",\"IsChannelPublished\":true,\"ChannelIconColorPrimaryUrl\":null,\"ChannelIconAndroidTvUrl\":null,\"Year\":2000,\"DurationSeconds\":1200,\"TimeshiftSeconds\":null,\"IsBlackouted\":false,\"IsLive\":false,\"IsPreviouslyShown\":false,\"IsRegionRestrictionEnabled\":false,\"IsTimeshiftEnabled\":true,\"IsCatchupEnabled\":true,\"IsNPvrEnabled\":true,\"IsLPvrEnabled\":false,\"RecordingId\":0,\"IsSeriesRecordingEnabled\":true,\"IsPpv\":false,\"HideInEpg\":false,\"VisibleOnlyIfBought\":false,\"Analytics\":{\"$type\":\"y.b.Tiles.Denormalization.TileAnalytics, y.b.Tiles.Contracts\",\"nur\":true},\"RegionRestrictions\":{\"$type\":\"System.Collections.Generic.List1[[y.b.Tiles.Denormalization.TileRegionRestriction, y.b.Tiles.Contracts]], mscorlib\",\"$values\":[]},\"PublishDate\":\"2019-10-14T02:02:58.813+02:00\",\"EndPublishDate\":\"9999-12-31T23:59:59.9999999+01:00\",\"Images\":{\"$type\":\"System.Collections.Generic.List1[[yGo.Sdk.Abstractions.DataModels.ImageDataModel, yGo.Sdk.Abstractions]], mscorlib\",\"$values\":[{\"Role\":\"photo\",\"Url\":\"http://aaa.com/images/828r_or1_191021_1535_7570e136_malcolm_mittendrin.jpg\",\"Description\":\"ORF / ©Twentieth Century Fox Film Corporation. All rights reserved.\",\"IsMain\":true,\"Width\":828,\"Height\":465,\"Type\":null,\"ExternalId\":null}]},\"Countries\":{\"$type\":\"System.Collections.Generic.List1[[yGo.Sdk.Abstractions.DataModels.CountryDataModel, yGo.Sdk.Abstractions]], mscorlib\",\"$values\":[{\"Codename\":\"usa\",\"Id\":361,\"Name\":\"USA\"}]},\"Categories\":{\"$type\":\"System.Collections.Generic.List1[[yGo.Sdk.Abstractions.DataModels.CategoryDataModel, yGo.Sdk.Abstractions]], mscorlib\",\"$values\":[{\"Codename\":\"serie\",\"Id\":20,\"Name\":\"Serie\",\"TypeCodename\":\"genre\"},{\"Codename\":\"comedyserie\",\"Id\":35,\"Name\":\"Comedyserie\",\"TypeCodename\":\"subcategory\"}]},\"Publications\":{\"$type\":\"System.Collections.Generic.List1[[yGo.Sdk.Abstractions.DataModels.PublicationPeriodDataModel, yGo.Sdk.Abstractions]], mscorlib\",\"$values\":[{\"From\":\"2019-10-14T02:02:58.813+02:00\",\"PlatformCodename\":\"android\",\"To\":\"9999-12-31T23:59:59.9999999+01:00\"},{\"From\":\"2019-10-14T02:02:58.813+02:00\",\"PlatformCodename\":\"www\",\"To\":\"9999-12-31T23:59:59.9999999+01:00\"},{\"From\":\"2019-10-14T02:02:58.813+02:00\",\"PlatformCodename\":\"ios\",\"To\":\"9999-12-31T23:59:59.9999999+01:00\"},{\"From\":\"2019-10-14T02:02:58.813+02:00\",\"PlatformCodename\":\"android-tv\",\"To\":\"9999-12-31T23:59:59.9999999+01:00\"}]},\"Toplists\":{\"$type\":\"System.Collections.Generic.List1[[yGo.Sdk.Abstractions.DataModels.ToplistDataModel, yGo.Sdk.Abstractions]], mscorlib\",\"$values\":[]},\"People\":{\"$type\":\"System.Collections.Generic.Dictionary2[[System.String, mscorlib],[System.Collections.Generic.List1[[yGo.Sdk.Abstractions.DataModels.PersonDataModel, yGo.Sdk.Abstractions]], mscorlib]], mscorlib\",\"director\":[{\"Id\":45508,\"Codename\":\"todd-holland\",\"FullName\":\"Todd Holland\",\"FirstName\":\"Todd\",\"LastName\":\"Holland\",\"RoleCodename\":\"director\",\"RoleName\":\"Regie\",\"FunctionDescription\":null}],\"musik\":[{\"Id\":129019,\"Codename\":\"they-might-be-giants\",\"FullName\":\"They Might Be Giants\",\"FirstName\":\"They\",\"LastName\":\"Might Be Giants\",\"RoleCodename\":\"musik\",\"RoleName\":\"Musik\",\"FunctionDescription\":null}],\"actor\":[{\"Id\":43782,\"Codename\":\"frankie-muniz\",\"FullName\":\"Frankie Muniz\",\"FirstName\":\"Frankie\",\"LastName\":\"Muniz\",\"RoleCodename\":\"actor\",\"RoleName\":\"Darsteller\",\"FunctionDescription\":\"Malcolm\"},{\"Id\":43783,\"Codename\":\"jane-kaczmarek\",\"FullName\":\"Jane Kaczmarek\",\"FirstName\":\"Jane\",\"LastName\":\"Kaczmarek\",\"RoleCodename\":\"actor\",\"RoleName\":\"Darsteller\",\"FunctionDescription\":\"Lois\"},{\"Id\":43784,\"Codename\":\"bryan-cranston\",\"FullName\":\"Bryan Cranston\",\"FirstName\":\"Bryan\",\"LastName\":\"Cranston\",\"RoleCodename\":\"actor\",\"RoleName\":\"Darsteller\",\"FunctionDescription\":\"Hal\"},{\"Id\":43785,\"Codename\":\"erik-per-sullivan\",\"FullName\":\"Erik Per Sullivan\",\"FirstName\":\"Erik\",\"LastName\":\"Per Sullivan\",\"RoleCodename\":\"actor\",\"RoleName\":\"Darsteller\",\"FunctionDescription\":\"Dewey\"},{\"Id\":43786,\"Codename\":\"christopher-kennedy-masterson\",\"FullName\":\"Christopher Kennedy Masterson\",\"FirstName\":\"Christopher\",\"LastName\":\"Kennedy Masterson\",\"RoleCodename\":\"actor\",\"RoleName\":\"Darsteller\",\"FunctionDescription\":\"Francis\"}],\"writer\":[{\"Id\":55589,\"Codename\":\"linwood-boomer\",\"FullName\":\"Linwood Boomer\",\"FirstName\":\"Linwood\",\"LastName\":\"Boomer\",\"RoleCodename\":\"writer\",\"RoleName\":\"Drehbuch\",\"FunctionDescription\":null},{\"Id\":45638,\"Codename\":\"victor-hammer\",\"FullName\":\"Victor Hammer\",\"FirstName\":\"Victor\",\"LastName\":\"Hammer\",\"RoleCodename\":\"writer\",\"RoleName\":\"Drehbuch\",\"FunctionDescription\":null}]},\"RelatedTiles\":{\"$type\":\"System.Collections.Generic.List1[[y.Sdk.Abstractions.DataModels.BaseTileDataModel, y.Sdk.Abstractions]], mscorlib\",\"$values\":[{\"Id\":\"prg.1684675\",\"Type\":\"prg\",\"OriginEntityId\":1684675,\"Codename\":\"orf1-1150013362\"},{\"Id\":\"prg.1678912\",\"Type\":\"prg\",\"OriginEntityId\":1678912,\"Codename\":\"orf1-1148618556\"},{\"Id\":\"prg.1672521\",\"Type\":\"prg\",\"OriginEntityId\":1672521,\"Codename\":\"orf1-1147720203\"},{\"Id\":\"prg.1672523\",\"Type\":\"prg\",\"OriginEntityId\":1672523,\"Codename\":\"orf1-1147720215\"},{\"Id\":\"prg.1674721\",\"Type\":\"prg\",\"OriginEntityId\":1674721,\"Codename\":\"orf1-1148617775\"},{\"Id\":\"prg.1674723\",\"Type\":\"prg\",\"OriginEntityId\":1674723,\"Codename\":\"orf1-1148617791\"},{\"Id\":\"prg.1676864\",\"Type\":\"prg\",\"OriginEntityId\":1676864,\"Codename\":\"orf1-1148618239\"},{\"Id\":\"prg.1676866\",\"Type\":\"prg\",\"OriginEntityId\":1676866,\"Codename\":\"orf1-1148618249\"},{\"Id\":\"prg.1686528\",\"Type\":\"prg\",\"OriginEntityId\":1686528,\"Codename\":\"orf1-1150579812\"},{\"Id\":\"prg.1686529\",\"Type\":\"prg\",\"OriginEntityId\":1686529,\"Codename\":\"orf1-1150579815\"},{\"Id\":\"prg.1686543\",\"Type\":\"prg\",\"OriginEntityId\":1686543,\"Codename\":\"orf1-1150579875\"},{\"Id\":\"prg.1686545\",\"Type\":\"prg\",\"OriginEntityId\":1686545,\"Codename\":\"orf1-1150579886\"},{\"Id\":\"prg.1678910\",\"Type\":\"prg\",\"OriginEntityId\":1678910,\"Codename\":\"orf1-1148618541\"},{\"Id\":\"prg.1684662\",\"Type\":\"prg\",\"OriginEntityId\":1684662,\"Codename\":\"orf1-1150013323\"},{\"Id\":\"prg.1674707\",\"Type\":\"prg\",\"OriginEntityId\":1674707,\"Codename\":\"orf1-1149472943\"}]},\"MediaFiles\":{\"$type\":\"System.Collections.Generic.List1[[y.Video.Contracts.Messages.MediaFileResult, y.Video.Contracts]], mscorlib\",\"$values\":[]},\"QualityLevels\":{\"$type\":\"System.Collections.Generic.List1[[y.b.Tiles.Denormalization.TileQualityLevel, y.b.Tiles.Contracts]], mscorlib\",\"$values\":[]},\"CatchupAvailableTo\":\"2019-10-28T14:40:00+01:00\",\"CatchupAvailableFrom\":\"2019-10-21T15:40:00+02:00\",\"Start\":\"2019-10-21T15:40:00+02:00\",\"Stop\":\"2019-10-21T16:00:00+02:00\",\"ChannelCodename\":\"orf1\",\"CanBuyInSvod\":false,\"Title\":\"Malcolm mittendrin\",\"OriginalTitle\":\"Malcolm in the Middle\",\"Description\":\"Der elfjährige Malcolm wächst gemeinsam mit seinen drei Brüdern in einer durchschnittlichen US-Familie auf. Was ihn von seinen Freunden unterscheidet, ist sein überdurchschnittlicher IQ von 165. Für Malcom ist das schlimmer, als radioaktiv verstrahlt zu sein. Sehr zu seinem Missfallen steckt ihn seine Mutter in eine Klasse für hochintelligente Kinder.\",\"ShortDescription\":\"Der elfjährige Malcolm wächst gemeinsam mit seinen drei Brüdern in einer durchschnittlichen US-Familie auf. Was ihn von seinen Freunden unterscheidet, ist sein überdurchschnittlicher IQ von 165. Für Malcom ist das schlimmer, als radioaktiv verstrahlt zu sein. Sehr zu seinem Missfallen steckt ihn seine Mutter in eine Klasse für hochintelligente Kinder.\",\"AgeRating\":0,\"IsAdultContent\":false,\"PurchaseOptions\":{\"$type\":\"System.Collections.Generic.List1[[y.b.Tiles.Denormalization.TilePurchaseOption, y.b.Tiles.Contracts]], mscorlib\",\"$values\":[]},\"AvailableAudioTracks\":{\"$type\":\"System.Collections.Generic.List1[[y.b.Tiles.Denormalization.TileMediaTrack, y.b.Tiles.Contracts]], mscorlib\",\"$values\":[]},\"AvailableSubtitles\":{\"$type\":\"System.Collections.Generic.List1[[y.b.Tiles.Denormalization.TileMediaTrack, y.b.Tiles.Contracts]], mscorlib\",\"$values\":[]},\"HasAudioDescription\":false,\"HasSignLanguage\":false,\"HasSubtitles\":false,\"Id\":\"prg.1684673\",\"Type\":\"prg\",\"OriginEntityId\":1684673,\"Codename\":\"orf1-1150013357\"}";

            var obj = NetJSON.DeserializeObject(str);
            Assert.IsNotNull(obj);
        }

        [TestMethod]
        public void ShouldBeDeserializeCharInClassObject()
        {
            var json = "{\"EVENTID\":19 ,\"APPL_EVENT\":\"\"}";
            var simple = NetJSON.Deserialize<CNTL_SIMPLE_EVENT>(json);
            Assert.AreEqual(simple.EVENTID, 19);
            Assert.IsTrue(simple.APPL_EVENT == '\0');
        }

        [TestMethod]
        public void ShouldThrowErrorForInvalidClassJSON()
        {
            //var json = "{\"Value\":\"\",\"Regex\":false}"; //good JSON
            var json = "{Value\":\"\",\"Regex\":false}"; //bad JSON
            var jsonChar = json.ToCharArray();
            var exception = default(Exception);
            try
            {
                var data = NetJSON.Deserialize<Dummy>(json);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsNotNull(exception, "Should throw invalid json exception");
        }

        [TestMethod]
        public void ShouldThrowErrorForInvalidDictionaryJSON()
        {
            //var json = "{\"Value\":\"\",\"Regex\":false}"; //good JSON
            var json = "{Value\":\"\",\"Regex\":false}"; //bad JSON
            var jsonChar = json.ToCharArray();
            var exception = default(Exception);
            try
            {
                var data = NetJSON.Deserialize<Dictionary<string, object>>(json);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsNotNull(exception, "Should throw invalid json exception");
        }

        [TestMethod]
        public void ShouldThrowErrorForInvalidDictionaryJSONNoQuote()
        {
            //var json = "{\"Value\":\"\",\"Regex\":false}"; //good JSON
            var json = "{Value:\"\",\"Regex\":false}"; //bad JSON
            var jsonChar = json.ToCharArray();
            var exception = default(Exception);
            try
            {
                var data = NetJSON.Deserialize<Dictionary<string, object>>(json);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsNotNull(exception, "Should throw invalid json exception");
        }
    }

    public class Dummy
    {
        public string Value { get; set; }
        public string Regex { get; set; }
    }

    public class CNTL_SIMPLE_EVENT
    {
        public int EVENTID;
        //public String APPL_EVENT; //Works perfect
        public char APPL_EVENT; //Causes Exception
    }

    public class TestFloatClass
    {
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public string StringValue { get; set; }
    }

    public class SimpleClass : ISimpleClass
    {
        public string SimpleClassProp { get; set; } = "Test";
        public string SimpleClassBaseProp { get; set; } = "Test";
    }

    [NetJSONKnownType(typeof(SimpleClass))]
    public interface ISimpleClass : ISimpleClassBase
    {
        string SimpleClassProp { get; set; }
    }

    [NetJSONKnownType(typeof(SimpleClass))]
    public interface ISimpleClassBase
    {
        string SimpleClassBaseProp { get; set; }
    }


    public class IntWithDefault
    {
        public int Id { get; set; }
    }

    public class IntNullableDefault
    {
        public int? Id { get; set; }
    }

    public class ULongClass
    {
        public ULongEnum LongEnum { get; set; }
    }

    public class UIntClass
    {
        public UIntEnum LongEnum { get; set; }
    }

    public class IntClass
    {
        public IntEnum LongEnum { get; set; }
    }

    [Flags]
    public enum ULongEnum : ulong
    {
        A = 100,
        ULongEnumValueA = 0x00400,
        ULongEnumValueB = 0x400000000
    }

    public enum UIntEnum : uint
    {
        ID, ID2 = uint.MaxValue, ID3 = 100000
    }

    public enum IntEnum : int
    {
        ID = 0, ID2 = -1000
    }

    public class UserDefinedCustomClass
    {
        public string Name { get; set; }

        public static void Serialize(UserDefinedCustomClass obj, StringBuilder sb, NetJSONSettings settings)
        {
            sb.AppendFormat("\"{{{0}}}\"", obj.Name);
        }

        public unsafe static UserDefinedCustomClass Deserialize(NetJSONStringReader reader, NetJSONSettings settings)
        {
            var sb = new StringBuilder();
            var current = '\0';
            while ((current = reader.Next()) != '}')
            {
                if(current == '{' || current == '"')
                {
                    continue;
                }

                sb.Append(current);
            }

            var name = sb.ToString();

            return new UserDefinedCustomClass { Name = name };
        }
    }

    public class MySuperStruct
    {
        public long Id { get; set; }
        public MySubStruct SubStruct { get; set; }
        public string SomeString { get; set; }
    }

    public class MySubStruct
    {
        public long Id { get; set; }
        public DateTime SomeDate { get; set; }
    }

    public class TestClassForCustomSerialization
    {
        public int ID { get; set; }
        public UserDefinedCustomClass Custom { get; set; }
    }

    public class EntityWithReadOnlyDictionary
    {
        public IReadOnlyDictionary<string, string> Map { get; set; }
    }

    public class EntityWithReadOnlyList
    {
        public IReadOnlyList<string> Strings { get; set; }
    }

    public class Entity
    {
        public IReadOnlyCollection<string> Items { get; set; }
    }

    public class NullableEntity
    {
        public Guid? Id { get; set; }
        public ValueObject? Value { get; set; }
    }

    public struct ValueObject
    {
        public string Value { get; set; }
    }


    public class EnabledClass
    {
        public EnabledClass()
        {
            Enabled = true;
        }

        public bool Enabled { get; set; }
    }

    public class YadroUser
    {
        public YadroUser()
        {
            Id = -1;
            CompanyId = -1;
            Email = String.Empty;
            Login = String.Empty;
            Password = String.Empty;
            Name = String.Empty;
            Surname = String.Empty;
            IsServerAdministrator = false;
            Enabled = true;
            HasWebAuthorisationAccess = true;
            UltimateUser = false;
            ToCreateOnCluster = true;
            UserTokenType = "GccObjects.Net.UserManagement.GccUserRemoteView";
            CultureId = 2057;
            DefaultTimeZone = 0;
            UseDaylightSaving = true;
        }

        /// <summary>
        /// Free to store your own data
        /// </summary>
        public string UserToken { get; set; }

        /// <summary>
        /// User's Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Id of the company where user belongs to
        /// </summary>
        public long CompanyId { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// User's login
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// User's password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// User's name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User's Surname
        /// </summary>
        public string Surname { get; set; }

        /// <summary>
        /// If the user is server administrator
        /// </summary>
        public bool IsServerAdministrator { get; set; }

        /// <summary>
        /// Has ability to be authorized via web
        /// </summary>
        public bool HasWebAuthorisationAccess { get; set; }

        /// <summary>
        /// If user is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Tells that this user .... Default is false.
        /// </summary>
        public bool UltimateUser { get; set; }

        /// <summary>
        /// 12 digits firma identificator
        /// </summary>
        public string FirmaIdentificator { get; set; }

        /// <summary>
        /// If user must be skipped to be created on the cluster
        /// </summary>
        public bool ToCreateOnCluster { get; set; }

        /// <summary>
        /// By default it contains UserRemoteView, but can be changed
        /// </summary>
        public string UserTokenType { get; set; }

        /// <summary>
        /// 2057 en-GB; 1049 ru-RU; 1031 de-DE; 1062 lv-LV; 1033 en-US etc...
        /// Default 2057
        /// </summary>       
        public int CultureId { get; set; }

        /// <summary>
        /// -12 0 +12 hours from GMT.
        /// Default 0
        /// </summary>       
        public int DefaultTimeZone { get; set; }
        /// <summary>
        /// If selected time zone uses daylight saving
        /// Default true
        /// </summary>
        public bool UseDaylightSaving { get; set; }
    }

    public class A
    {
        public A()
        {
            Details = new Dictionary<int, B>();
        }

        public Dictionary<int, B> Details { get; set; }
    }

    public class B
    {
    }

    struct SimpleObjectStruct
    {
        public int ID;
        public string Name;
        public string Value;
    }

    public class Test
    {
        public string Val { get; set; }
    }


    public class API_ImportResult
    {
        public class CoinResults
        {
            public string OrderUuid { get; set; }
            public string Exchange { get; set; }
            public string TimeStamp { get; set; }
            public string OrderType { get; set; }
            public string Limit { get; set; }
            public string Quantity { get; set; }
            public string QuantityRemaining { get; set; }
            public string Commission { get; set; }
            public string Price { get; set; }
            public string PricePerUnit { get; set; }
            public bool IsConditional { get; set; }
            public string Condition { get; set; }
            public bool ImmediateOrCancel { get; set; }
            public string Closed { get; set; }
        }

        public bool success { get; set; }
        public string message { get; set; }
        public ICollection<CoinResults> result { get; set; }
    }

    internal abstract class PersonEx
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public int Id { get; set; }
    }

    internal sealed class User : PersonEx
    {
        public UserStatus Status { get; set; }

        public AccountType AccountType { get; set; }
    }

    internal enum AccountType
    {
        Internal, External, Demo
    }

    internal enum UserStatus
    {
        Active, Inactive, Suspended, Pending
    }

    public class TestClassWithIgnoreAttr
    {
        [TestIgnore]
        public int ID { get; set; }
    }

    public class TestIgnoreAttribute : Attribute
    {
    }

    public class XmlTestClass {
        [XmlElement(ElementName = "XmlName")]
        public string Name { get; set; }
    }

    public class CustomerResult
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Id { get; set; }
    }

    public class Result<T>
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int TotalResults { get; set; }
        public T Data { get; set; }
    }

    [Flags]
    public enum TestFlags
    {
        A = 1,
        B = 2,
        C = 4
    }

    public class FooA
    {
        public int Type
        { get; set; }

        public int IntVal
        { get; set; }

        public TestFlags EnumVal
        { get; set; }
    }

    public class MicrosoftJavascriptSerializerTestData
    {
        public int CreatorId { get; set; }
        public DateTime udtCreationDate { get; set; }
    }

    public class EnumHolder
    {
        public ByteEnum BEnum { get; set; }
        public ShortEnum SEnum { get; set; }
    }

    public enum ByteEnum : byte
    {
        V1 = 1,
        V2 = 2
    }

    public enum ShortEnum : short
    {
        V1 = 1,
        V2 = 2
    }

    [Serializable]
    public class ComplexObject
    {
        static RandomBufferGenerator generator = new RandomBufferGenerator(65000);

        public ComplexObject()
        {
            Thing1 = true;
            Thing2 = int.MaxValue;
            Thing3 = 'q';
            Thing4 = "asdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasasdfasdfasas";
            Thing5 = new Dictionary<string, string>()
        {
            { "1", RandomBufferGenerator.RandomString(4) },
            { "2", RandomBufferGenerator.RandomString(4) },
        };
            Thing6 = generator.GenerateBufferFromSeed(32000);
            Thing7 = uint.MaxValue;
        }

        public bool Thing1 { get; set; }

        public int Thing2 { get; set; }

        public char Thing3 { get; set; }

        public string Thing4 { get; set; }

        public Dictionary<string, string> Thing5 { get; set; }

        public byte[] Thing6 { get; set; }

        public uint Thing7 { get; set; }
    }

    [System.Diagnostics.DebuggerStepThrough]
    public class RandomBufferGenerator
    {
        private readonly Random _random = new Random();
        private readonly byte[] _seedBuffer;

        public RandomBufferGenerator(int maxBufferSize)
        {
            _seedBuffer = new byte[maxBufferSize];

            _random.NextBytes(_seedBuffer);
        }

        public byte[] GenerateBufferFromSeed(int size)
        {
            int randomWindow = _random.Next(0, size);

            byte[] buffer = new byte[size];

            Buffer.BlockCopy(_seedBuffer, randomWindow, buffer, 0, size - randomWindow);
            Buffer.BlockCopy(_seedBuffer, 0, buffer, size - randomWindow, randomWindow);

            return buffer;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public static string RandomString(int length)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] stringChars = new char[length];
            Random random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
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

    public class Tracker
    {
        [NetJSONProperty("Tracker_Name")]
        public string Name { get; set; }

        [NetJSONProperty("Profile_Tracker_ID")]
        public int ID { get; set; }

        [NetJSONProperty("Tracker_ContentType")]
        public string ContentType { get; set; }

        [NetJSONProperty("Tracker_SearchTerm")]
        public string SearchTerm { get; set; }

        [NetJSONProperty("Tracker_SortBy")]
        public string SortBy { get; set; }

        [NetJSONProperty("Tracker_Facets")]
        public System.Collections.Generic.List<Facet> FacetCollection { get; set; }
    }

    public class Facet
    {
        public Facet() { }

        public Facet(string _facet)
        {
            //Value = _facet;
        }

        [NetJSONProperty("Profile_Tracker_Facets_ID")]
        public int ID { get; set; }

        [NetJSONProperty("Profile_Tracker_ID")]
        public int TrackerID { get; set; }

        [NetJSONProperty("Tracker_Facet")]
        public Guid Value { get; set; }
    }

    public interface IPerson
    {
        string Name { get; set; }
    }

    public abstract class PersonAbstract
    {
        public abstract string Name { get; set; }
    }

    public class PersonX : IPerson
    {
        public string Name { get; set; }
    }

    public class PersonX2 : PersonAbstract
    {
        public override string Name { get; set; }
    }

    public struct EventStruct
    {
        public EventStruct(Guid id, PayloadStruct payload, int version, DateTimeOffset created) { Id = id; Payload = payload; Version = version; Created = created; }

        public Guid Id { get; }
        public PayloadStruct Payload { get; }
        public int Version { get; }
        public DateTimeOffset Created { get; }
    }

    public struct PayloadStruct
    {
        public PayloadStruct(string value) { Value = value; }

        public string Value { get; }
    }

    public struct EventStructWithPrivateSetters
    {
        public EventStructWithPrivateSetters(Guid id, PayloadStructWithPrivateSetter payload, int version, DateTimeOffset created) { Id = id; Payload = payload; Version = version; Created = created; }

        public Guid Id { get; private set; }
        public PayloadStructWithPrivateSetter Payload { get; private set; }
        public int Version { get; private set; }
        public DateTimeOffset Created { get; private set; }
    }

    public struct PayloadStructWithPrivateSetter
    {
        public PayloadStructWithPrivateSetter(string value) { Value = value; }

        public string Value { get; private set; }
    }

    public struct EventStructWithReadOnlyBackingFields
    {
        private readonly Guid _id;
        private readonly PayloadStructWithReadOnlyBackingField _payload;
        private readonly int _version;
        private readonly DateTimeOffset _created;

        public EventStructWithReadOnlyBackingFields(Guid id, PayloadStructWithReadOnlyBackingField payload, int version, DateTimeOffset created) { _id = id; _payload = payload; _version = version; _created = created; }

        public Guid Id => _id;
        public PayloadStructWithReadOnlyBackingField Payload => _payload;
        public int Version => _version;
        public DateTimeOffset Created => _created;
    }

    public struct PayloadStructWithReadOnlyBackingField
    {
        private readonly string _value;

        public PayloadStructWithReadOnlyBackingField(string value) { _value = value; }

        public string Value => _value;
    }

    public class EventClass
    {
        public EventClass(Guid id, PayloadClass payload, int version, DateTimeOffset created) {
            Id = id; Payload = payload; Version = version; Created = created;
        }

        public Guid Id { get; }
        public PayloadClass Payload { get; }
        public int Version { get; }
        public DateTimeOffset Created { get; }
    }

    public class PayloadClass
    {
        public PayloadClass(string value) { Value = value; }

        public string Value { get; }
    }

    public class EventClassWithPrivateSetters
    {
        public EventClassWithPrivateSetters(Guid id, PayloadClassWithPrivateSetter payload, int version, DateTimeOffset created) { Id = id; Payload = payload; Version = version; Created = created; }

        public Guid Id { get; private set; }
        public PayloadClassWithPrivateSetter Payload { get; private set; }
        public int Version { get; private set; }
        public DateTimeOffset Created { get; private set; }
    }

    public class PayloadClassWithPrivateSetter
    {
        public PayloadClassWithPrivateSetter(string value) { Value = value; }

        public string Value { get; private set; }
    }

    public class EventClassWithReadOnlyBackingFields
    {
        private readonly Guid _id;
        private readonly PayloadClassWithWithReadOnlyBackingField _payload;
        private readonly int _version;
        private readonly DateTimeOffset _created;

        public EventClassWithReadOnlyBackingFields(Guid id, PayloadClassWithWithReadOnlyBackingField payload, int version, DateTimeOffset created) { _id = id; _payload = payload; _version = version; _created = created; }

        public Guid Id => _id;
        public PayloadClassWithWithReadOnlyBackingField Payload => _payload;
        public int Version => _version;
        public DateTimeOffset Created => _created;
    }

    public class PayloadClassWithWithReadOnlyBackingField
    {
        private readonly string _value;

        public PayloadClassWithWithReadOnlyBackingField(string value) { _value = value; }

        public string Value => _value;
    }
}
