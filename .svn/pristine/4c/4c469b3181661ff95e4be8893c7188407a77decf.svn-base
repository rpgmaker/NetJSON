using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NetJSON.Core.Tests
{

    public class TestClass
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
    public class NetJSONCoreTest
    {
        [Fact]
        public void CanSerializeTestData()
        {
            var data = new TestClass { ID = 1000, Name = "This is a test" };
            var json = NetJSON.Serialize(data);
            var data2 = NetJSON.Deserialize<TestClass>(json);
            Assert.Equal(data.ID, data2.ID);
        }
    }
}
