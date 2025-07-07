using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Xunit;

namespace NetJSON.Standard2_0.Tests
{
    public class Query
    {
        public string query;
        public dynamic variables;
    }

    public class Person
    {
        public int PersonId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public DateTime DateOfBirth { get; set; }
        public byte[] HashValue { get; set; }
    }

    public class NetJSONTest
    {
        [Fact]
        public void TestPersonObject()
        {
            var items = new List<Person>
            {
                new Person
                {
                    PersonId = 1,
                    Firstname = "Test",
                    Lastname = "Test2",
                    DateOfBirth = DateTime.Now,
                    HashValue = new byte[0]
                }
            };

            var json = NetJSON.Serialize(items);
            Assert.NotNull(json);
        }

        [Fact]
        public void TestQueryObject()
        {
            var graphqltest = new Query
            {
                query = @"query ($id:Int, $user:String, $type: MediaType) {
    MediaListCollection(userId:$id, userName: $user, type: $type) {
      lists {
        name
        entries {
          media {
            title {
              romaji
              english
              userPreferred
            }
            startDate {
              year
            }
            id
            idMal
            status
            season
            format
          }
        }
      }
    }
  }
            ",
                variables = new Dictionary<string, object>()
                {
                    ["id"] = 170636,
                    ["user"] = "gilbatir",
                    ["type"] = "ANIME"
                }
            };
            string test = NetJSON.Serialize(graphqltest);
            StringContent content = new StringContent(test, Encoding.UTF8, "application/json");
            string str = "{\"data\":{\"MediaListCollection\":{\"lists\":[{\"name\":\"Rewatching\",\"entries\":[{\"media\":{\"title\":{\"romaji\":\"Shuumatsu Nani Shitemasu ka? Isogashii desu ka? Sukutte Moratte Ii desu ka?\",\"english\":\"WorldEnd: What do you do at the end of the world? Are you busy? Will you save us?\",\"userPreferred\":\"WorldEnd: What do you do at the end of the world? Are you busy? Will you save us?\"},\"startDate\":{\"year\":2017},\"id\":21860,\"idMal\":33502,\"status\":\"FINISHED\",\"season\":\"SPRING\",\"format\":\"TV\"}}]},{\"name\":\"Completed\",\"entries\":[{\"media\":{\"title\":{\"romaji\":\"Ano Hi Mita Hana no Namae wo Bokutachi wa Mada Shiranai.\",\"english\":\"Anohana: The Flower We Saw That Day\",\"userPreferred\":\"Anohana: The Flower We Saw That Day\"},\"startDate\":{\"year\":2011},\"id\":9989,\"idMal\":9989,\"status\":\"FINISHED\",\"season\":\"SPRING\",\"format\":\"TV\"}},{\"media\":{\"title\":{\"romaji\":\"Ano Hi Mita Hana no Namae wo Bokutachi wa Mada Shiranai. Movie\",\"english\":\"Anohana: The Flower We Saw That Day Movie\",\"userPreferred\":\"Anohana: The Flower We Saw That Day Movie\"},\"startDate\":{\"year\":2013},\"id\":15039,\"idMal\":15039,\"status\":\"FINISHED\",\"season\":\"SUMMER\",\"format\":\"MOVIE\"}}]}]}}}";
            var x = NetJSON.DeserializeObject(str);

            Assert.NotNull(x);
        }
    }
}
