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
        public int id { get; set; }
        public int icon { get; set; }
        public string name { get; set; }
        public string name_en { get; set; }
        public string name_fr { get; set; }
        public string name_de { get; set; }
        public string name_ja { get; set; }
    }

    [JsonObject("statusDetail")]
    class StatusDetail
    {
        public string icon { get; set; }
    }

    [JsonObject("status_en")]
    class Status_En
    {
        public int id { get; set; }
        public int icon { get; set; }
        public string name { get; set; }
        public string name_en { get; set; }
    }
    [JsonObject("status_fr")]
    class Status_Fr
    {
        public int id { get; set; }
        public int icon { get; set; }
        public string name { get; set; }
        public string name_fr { get; set; }
    }
    [JsonObject("status_de")]
    class Status_De
    {
        public int id { get; set; }
        public int icon { get; set; }
        public string name { get; set; }
        public string name_de { get; set; }
    }
    [JsonObject("status_ja")]
    class Status_Ja
    {
        public int id { get; set; }
        public int icon { get; set; }
        public string name { get; set; }
        public string name_ja { get; set; }
    }

}
