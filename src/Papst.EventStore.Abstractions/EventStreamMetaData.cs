using System;
using System.Collections.Generic;

namespace Papst.EventStore.Abstractions
{
    /// <summary>
    /// Meta Data Object of a Event Stream Document
    /// </summary>
    public class EventStreamMetaData
    {
        /// <summary>
        /// Id of the User that caused the Document
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Name of the User that caused the Document
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Optional: TenantId
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Comment if available
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Additional Meta Data Properties
        /// </summary>
        public Dictionary<string, string> Additional { get; set; }
    }
}