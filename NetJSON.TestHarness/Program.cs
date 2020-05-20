using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJSON.TestHarness
{
    public class Dummy
    {
        public string Value { get; set; }
        public string Regex { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Define invalid JSON");
            //var json = "{\"Value\":\"\",\"Regex\":false}"; //good JSON
            var json = "{\"Value:\"\",\"Regex\":false}"; //bad JSON
            Console.WriteLine("Run Deserialize");
            var simple2 = NetJSON.Deserialize<Dummy>(json);
            
            Console.WriteLine("Done");
        }
    }
}
