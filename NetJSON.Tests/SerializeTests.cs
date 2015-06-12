using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NetJSON.Tests
{
    [TestClass]
    public class SerializeTests
    {
        public enum MyEnumTest {
            Test1,Test2
        }

        [TestMethod]
        public void CanSerializeMccUserDataObject() {
            var obj = new MccUserData() { arr = new int?[]{10, null, 20} };

            NetJSON.IncludeFields = true;
            NetJSON.GenerateAssembly = true;

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
            NetJSON.GenerateAssembly = true;

            var json = NetJSON.Serialize(dict);
        }

        [TestMethod]
        public void StringSkippingCauseInfiniteLoop() {
            string jsonData = "{\"jsonrpc\":\"2.0\",\"result\":{\"availableToBetBalance\":602.15,\"exposure\":0.0,\"retainedCommission\":0.0,\"exposureLimit\":-10000.0,\"discountRate\":2.0,\"pointsBalance\":1181,\"wallet\":\"UK\"},\"id\":1}";

            var data = NetJSON.Deserialize<JsonRpcResponse<AccountFundsResponse>>(jsonData);
        }

        [TestMethod]
        public void NestedGraphDoesNotThrow()
        {
            var o = new GetTopWinsResponse()
            {
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
        }
    }

    public enum ExceptionType
    {
        None,
        Business,
        Security,
        EarlierRequestAlreadyFailed,
        Unknown,
    }

    public class ExceptionInfo
    {
        public ExceptionInfo InnerException { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public string Type { get; set; }

        public string FaultCode { get; set; }

        public ExceptionInfo()
        {
        }

        public ExceptionInfo(Exception exception)
        {
            this.Message = exception.Message;
            this.StackTrace = exception.StackTrace;
            this.Type = exception.GetType().ToString();
            if (exception.InnerException == null)
                return;
            this.InnerException = new ExceptionInfo(exception.InnerException);
        }
    }

    public class Response
    {
        public ExceptionInfo Exception { get; set; }

        public ExceptionType ExceptionType { get; set; }

        public bool IsCached { get; set; }

        public Response GetShallowCopy()
        {
            return (Response)this.MemberwiseClone();
        }
    }

    public class GetTopWinsResponse : Response
    {
        public GetTopWinsResponse()
        {
            //TopWins = new List<TopWinDto>();
        }

        public IEnumerable<TopWinDto> TopWins { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var win in TopWins)
                sb.AppendLine(win.ToString());

            return sb.ToString();
        }
    }

    public class TopWinDto
    {
        public TopWinType Type { get; set; }

        public DateTime Timestamp { get; set; }

        public string Nickname { get; set; }

        public decimal Amount { get; set; }

        public TopWinOnlineCasino OnlineCasino { get; set; }

        public TopWinLandBasedCasino LandBasedCasino { get; set; }

        public TopWinOnlineSports OnlineSports { get; set; }
    }

    public class TopWinOnlineCasino
    {
        public string GameId { get; set; }
    }

    public class TopWinLandBasedCasino
    {
        public string Location { get; set; }

        public string MachineName { get; set; }
    }

    public class TopWinOnlineSports
    {
        public DateTime CreationDate { get; set; }

        public string HomeTeam { get; set; }

        public string AwayTeam { get; set; }

        public int Odds { get; set; }

        public int BranchId { get; set; }

        public int LeagueId { get; set; }

        public string YourBet { get; set; }

        public string LeagueName { get; set; }
    }

    public enum TopWinType
    {
        OnlineCasinoWin,
        OnlineSportsWin,
        LandBasedCasinoWin
    }
}
