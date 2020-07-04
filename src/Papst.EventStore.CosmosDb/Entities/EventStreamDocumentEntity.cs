using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Papst.EventStore.Abstractions;
using System;

namespace Papst.EventStore.CosmosDb.Entities
{
    /// <summary>
    /// CosmosDb Entity that represents a <see cref="EventStreamDocument"/>
    /// </summary>
    public class EventStreamDocumentEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The Unique Event Id
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// The Event Stream Id
        /// </summary>
        [JsonProperty("StreamId")]
        public Guid StreamId { get; set; }

        /// <summary>
        /// Type of the Document
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public EventStreamDocumentType DocumentType { get; set; }

        /// <summary>
        /// Version of the Document after applying this Event
        /// </summary>
        public ulong Version { get; set; }

        /// <summary>
        /// The Time of the Event
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// Name of the Event
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Data of the Event as JSON Object
        /// </summary>
        public JObject Data { get; set; }

        /// <summary>
        /// Type of the Data
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// The type on which the Event will be applied
        /// </summary>
        public string TargetType { get; set; }

        /// <summary>
        /// Metadata for the Event
        /// </summary>
        public EventStreamMetaData MetaData { get; set; }
    }
}
