using System;
using System.Collections.Generic;
using System.IO;
using Cactus.Chat.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using Netcore.Simplest.Chat.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netcore.Simplest.Chat.Test.Unit
{
    [TestClass]
    public class JsonSerializerTest
    {
        [TestMethod]
        public void DateTimeTest()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                // DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
            var serializer = JsonSerializer.Create(serializerSettings);

            var obj = (JObject)serializer.Deserialize(new StringReader("{tst:'2017-03-20T15:22:06.354Z'}"), typeof(JObject));
            Assert.IsNotNull(obj["tst"]);
            var datetime = obj["tst"].Value<DateTime>();
            var sw = new StringWriter();
            serializer.Serialize(sw, new { tst = datetime });
            var str = sw.ToString();
            Assert.IsTrue(str.Contains("\"2017-03-20T15:22:06.354Z\""));
        }

        [TestMethod]
        public void ChatObjectTest()
        {
            var chat = new Chat<CustomIm, CustomProfile>
            {
                Id = ObjectId.GenerateNewId().ToString(),
                MessageCount = 1,
                LastActivityOn = DateTime.UtcNow,
                Participants = new List<ChatParticipant<CustomProfile>>
                {
                    new ChatParticipant<CustomProfile>
                    {
                        Id= ObjectId.GenerateNewId().ToString(),
                        DeliveredOn=DateTime.UtcNow,
                        HasLeft=false,
                        IsDeleted=false,
                        Profile=new CustomProfile
                        {
                            FirstName="Nobody"
                        }
                    }
                },
                Title = "title",
                StartedBy = ObjectId.GenerateNewId().ToString(),
                StartedOn = DateTime.UtcNow
            };
            var str = JsonConvert.SerializeObject(chat, Formatting.Indented);
            var desChat = JsonConvert.DeserializeObject<Chat<CustomIm, CustomProfile>>(str);
            Assert.AreEqual(chat.Id, desChat.Id);
        }
    }
}
