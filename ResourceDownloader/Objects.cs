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
        [JsonProperty("iconURI")]
        public string IconURI { get; set; }
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

namespace ResourceDownloader.Model.XIVAPI
{
    [JsonObject("StatusResultSet")]
    public class StatusResultSet
    {
        [JsonProperty("Pagination")]
        public Pagination Pagination { get; set; }
        [JsonProperty("Results")]
        public List<Status> Results { get; set; }
    }

    [JsonObject("Status")]
    public class Status
    {
        [JsonProperty("ID")]
        public int ID { get; set; }
        [JsonProperty("Icon")]
        public string Icon { get; set; }
        [JsonProperty("Name_de")]
        public string Name_de { get; set; }
        [JsonProperty("Name_en")]
        public string Name_en { get; set; }
        [JsonProperty("name_fr")]
        public string Name_fr { get; set; }
        [JsonProperty("Name_ja")]
        public string Name_ja { get; set; }
    }

    [JsonObject("Pagination")]
    public class Pagination
    {
        [JsonProperty("Page")]
        public int Page { get; set; }
        [JsonProperty("PageNext")]
        public int PageNext { get; set; }
        [JsonProperty("PagePrev")]
        public int PagePrev { get; set; }
        [JsonProperty("PageTotal")]
        public int PageTotal { get; set; }
        [JsonProperty("Results")]
        public int Results { get; set; }
        [JsonProperty("ResultsPerPage")]
        public int ResultsPerPage { get; set; }
        [JsonProperty("ResultsTotal")]
        public int ResultsTotal { get; set; }
    }

}