﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Data.States;
using Data.Infrastructure.JsonNet;

namespace Data.Interfaces.DataTransferObjects
{
    public class CrateDTO
    {
        [JsonProperty("manifestType")]
        public string ManifestType { get; set; }

        [JsonProperty("manifestId")]
        public int ManifestId { get; set; }

        [JsonProperty("manufacturer")]
        public ManufacturerDTO Manufacturer { get; set; }

        [JsonProperty("manifestRegistrar")]
        public string ManifestRegistrar
        {
            get { return "www.fr8.co/registry"; }
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("contents")]
        public JToken Contents { get; set; }

        [JsonProperty("parentCrateId")]
        public string ParentCrateId { get; set; }

        [JsonProperty("createTime")]
        public DateTime CreateTime { get; set; }

        [JsonProperty("availability")]
        [JsonConverter(typeof(AvailabilityConverter))]
        public AvailabilityType Availability { get; set; }
    }
}
