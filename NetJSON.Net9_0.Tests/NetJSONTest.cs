#nullable enable
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace NetJSON.Net9_0.Tests
{
    public class Person
    {
        public int Id { get; set; }

        public List<Address> Addresses { get; set; } // basically any class type
    }
    public class Address 
    {
        public int Id { get; set; } = 1;
    }
    
    public class NetJSONTest
    {
        [Fact]
        public void Test1()
        {
            var jsonString = "{\"All\":{\"ym\":\"ss\"},\"ervicesAuto\":{\"ym\":\"ss\"},\"ocessor\":{\"\":\"ss\"},\"sonalDevice\":{\"\":\"ss\"},\"rryCan\":{\"\":\"ss\"},\"licom\":{\"ymr\":\"ss\"}}";
            var result = NetJSON.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonString);
            Assert.True(result.ContainsKey("All") && result["All"].ContainsKey("ym") && result["All"]["ym"].Equals("ss"));
        }

        [Fact]
        public void TestNullValueInArrayShouldBeSkipped()
        {
            var p = new Person()
            {
                Id = 1,
                Addresses = new List<Address> { new Address(), null, new Address() }
            };

            var json = NetJSON.Serialize(p);
            var p2 = NetJSON.Deserialize<Person>(json);
            Assert.True(p2.Addresses.Count == 3);
        }

        [Fact]
        public void TestForNullShouldNotThrowInvalidJson()
        {
            Person nullModel = null;
            var json = NetJSON.Serialize(nullModel, NetJSONSettings.CurrentSettings);
            var result = NetJSON.Deserialize<Person>(json);
            Assert.Null(result);
        }

        [Fact]
        public void TestForNullabeShouldNotThrowNullException()
        {
            Stream? nullableModal = null;
            var json = NetJSON.Serialize(nullableModal);
            Assert.Equal("null", json);
        }
    }
}
