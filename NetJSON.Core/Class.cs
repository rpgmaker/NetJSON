using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetJSON.Core
{
    public class Class
    {
        public static void Main() {
            var t = typeof(Math);
            var info = t.GetTypeInfo();
            var m = typeof(SecurityPermissionAttribute);
        }
    }
}
