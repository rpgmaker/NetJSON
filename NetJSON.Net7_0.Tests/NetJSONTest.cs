using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NetJSON.Net7_0.Tests
{
    public class NetJSONTest
    {
        [Fact]
        public void TestDictionary()
        {
            var jsonString = "{\"All\":{\"ym\":\"ss\"},\"ervicesAuto\":{\"ym\":\"ss\"},\"ocessor\":{\"\":\"ss\"},\"sonalDevice\":{\"\":\"ss\"},\"rryCan\":{\"\":\"ss\"},\"licom\":{\"ymr\":\"ss\"}}";
            var result = NetJSON.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonString);
            Assert.True(result.ContainsKey("All") && result["All"].ContainsKey("ym") && result["All"]["ym"].Equals("ss"));
        }

        [Fact]
        public void TestSimpleObject()
        {
            var o = new SimpleObject() { ID = 100, Name = "Test", Value = "Value" };
            var output = NetJSON.Serialize(o);
            var newObject = NetJSON.Deserialize<SimpleObject>(output);
            
            Assert.Equal(o.ID, newObject.ID);
            Assert.Equal(o.Name, newObject.Name);
            Assert.Equal(o.Value, newObject.Value);
        }
        
        [Fact]
        public void TestSimpleObjectStruct()
        {
            var o = new SimpleObjectStruct() { ID = 100, Name = "Test", Value = "Value" };
            var output = NetJSON.Serialize(o);
            var newObject = NetJSON.Deserialize<SimpleObjectStruct>(output);
            
            Assert.Equal(o.ID, newObject.ID);
            Assert.Equal(o.Name, newObject.Name);
            Assert.Equal(o.Value, newObject.Value);
        }

        [Fact]
        public void TestBuiltInClassFaker()
        {
            var obj = new BuiltInClassFaker(5).Generate();
            var json = NetJSON.Serialize(obj);
            var o = NetJSON.Deserialize<BuiltInClass>(json);
            Assert.Equal(o.Char, obj.Char);
        }
    }
}
