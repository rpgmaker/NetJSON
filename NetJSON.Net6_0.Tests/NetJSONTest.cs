using System;
using System.Collections.Generic;
using Xunit;

namespace NetJSON.Net6_0.Tests
{
    public class NetJSONTest
    {
        [Fact]
        public void Test1()
        {
            var jsonString = "{\"All\":{\"ym\":\"ss\"},\"ervicesAuto\":{\"ym\":\"ss\"},\"ocessor\":{\"\":\"ss\"},\"sonalDevice\":{\"\":\"ss\"},\"rryCan\":{\"\":\"ss\"},\"licom\":{\"ymr\":\"ss\"}}";
            var result = NetJSON.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonString);
            Assert.True(result.ContainsKey("All") && result["All"].ContainsKey("ym") && result["All"]["ym"].Equals("ss"));
        }
    }
}
