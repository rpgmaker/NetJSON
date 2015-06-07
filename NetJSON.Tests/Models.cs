using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJSON.Tests {
    public class ErrorData {
        public int code { get; set; }

        public string message { get; set; }
    }

    public class JsonRpcResponse<T> {
        public T result { get; set; }

        public ErrorData error { get; set; }

        public bool IsError {
            get { return this.error != null; }
        }

        public int id { get; set; }
    }

    public class AccountFundsResponse {
        public double availableToBetBalance { get; set; }

        public double exposure { get; set; }

        public double retainedCommission { get; set; }

        public double exposureLimit { get; set; }
    }
}
