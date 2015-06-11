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


    public class MccUserData {
        public MccUserData() {

        }

        public int ifjhklfdfjlkdjfldgfdgdgdfgdgdgdf = 0;

        public enum eTestE {
            One = 1,
            Two = 2,
            Three = 1,
        }

        public eTestE TestEnum { get; set; }

        public TType1 tt1 { get; set; }

        public long CustomerId { get; set; }

        public long HostId { get; set; }

        public string Login { get; set; }

        public bool Active { get; set; }

        public DateTime DtuCreated { get; set; }

        public DateTime ExpirationDate { get; set; }
        public DateTime? ExpirationDate1 { get; set; }

        public bool LoggingPermission { get; set; }


        public string Language { get; set; }

        public int CultureId { get; set; }

        public bool IsAppSubscriber { get; set; }

        public byte[] RowKey { get; set; }

        public HashSet<string> SubscribedApps { get; set; }


        public string CompanyBindingId { get; set; }

        public bool IDT_Enabled { get; set; }

        public DateTime IDT_ExpirationDate { get; set; }

        public bool IDT_PlayStore { get; set; }

        public float FloaTest { get; set; }
        public double DoubleTest { get; set; }
        public decimal DecimalTest { get; set; }
        public double DoubleNullTest { get; set; }
        public char CharTest { get; set; }

        public Dictionary<int, string> rD { get; set; }

        public Dictionary<int, TType1> rObj { get; set; }

        public int?[] arr { get; set; }

        public List<string> sdt { get; set; }

        public string TestString { get; set; }

        public sbyte TestByte { get; set; }

        public TimeSpan TestTimeSpan { get; set; }

        public Guid TestGuid { get; set; }

        public int[][] arr1 { get; set; }
    }

    public class TType1 {
        public long P1 = 12;
        public float P2 = 4.5f;
        public int? P4 = null;
    }
}
