using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApp
{
    public class Commit
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("Username")]
        public string Username { get; set; }

        [JsonProperty("Timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
