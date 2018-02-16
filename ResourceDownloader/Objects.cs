using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ResourceDownloader.Model
{
    [JsonObject("status")]
    class Status
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("icon")]
        public int Icon { get; set; }
        [JsonProperty("iconFileName")]
        public string IconFileName { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("name_en")]
        public string Name_en { get; set; }
        [JsonProperty("name_fr")]
        public string Name_fr { get; set; }
        [JsonProperty("name_de")]
        public string Name_de { get; set; }
        [JsonProperty("name_ja")]
        public string Name_ja { get; set; }
    }

    [JsonObject("statusDetail")]
    class StatusDetail
    {
        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    [JsonObject("status")]
    class StatusSummary
    {
        [JsonProperty("iconFileName")]
        public string IconFileName { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }

}
