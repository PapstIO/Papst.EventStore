using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;

namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Representation of an Event Stream Entry
    /// </summary>
    public class EventStreamDocument
    {
        /// <summary>
        /// The Unique Event Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The Event Stream Id
        /// </summary>
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
        public Type DataType { get; set; }

        /// <summary>
        /// The type on which the Event will be applied
        /// </summary>
        public Type TargetType { get; set; }

        /// <summary>
        /// Metadata for the Event
        /// </summary>
        public EventStreamMetaData MetaData { get; set; }
    }
}
