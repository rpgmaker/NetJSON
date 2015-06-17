using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetJSON.V3_5.Tests {
    [TestClass]
    public class SerializeTests {
        [TestMethod]
        public void CanSerializeSimpleObject() {
            NetJSON.GenerateAssembly = true;
            var simple = new SimpleObject { MyInt = 1000, MyString = "Hello World" };

            var json = NetJSON.Serialize(simple);
            var simple2 = NetJSON.Deserialize<SimpleObject>(json);
        }
    }

    public class SimpleObject {
        public string MyString { get; set; }
        public int MyInt { get; set; }
    }
}
