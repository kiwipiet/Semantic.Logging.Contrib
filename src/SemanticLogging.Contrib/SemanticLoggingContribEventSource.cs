using System;
using System.Diagnostics.Tracing;

namespace SemanticLogging.Contrib
{
    /// <summary>
    /// An <see cref="EventSource"/> class to notify non-transient faults and internal trace information.
    /// </summary>
    [EventSource(Name = "SemanticLoggingContrib", LocalizationResources = "SemanticLogging.Contrib.SemanticLoggingContribEventSourceResources")]
    public sealed class SemanticLoggingContribEventSource : EventSource
    {
        private static readonly Lazy<SemanticLoggingContribEventSource> Instance = new Lazy<SemanticLoggingContribEventSource>(() => new SemanticLoggingContribEventSource());

        private SemanticLoggingContribEventSource()
        {
        }

        /// <summary>
        /// Gets the singleton instance of <see cref="SemanticLoggingContribEventSource"/>.
        /// </summary>
        /// <value>The singleton instance.</value>
        public static SemanticLoggingContribEventSource Log => Instance.Value;

        [Event(1, Level = EventLevel.Critical, Keywords = Keywords.Sink, Message = "A eventlog sink failed to write an event. Message: {0}")]
        internal void EventLogSinkWriteFailed(string message)
        {
            if (IsEnabled(EventLevel.Critical, Keywords.Sink))
            {
                WriteEvent(300, message);
            }
        }

        /// <summary>
        /// Custom defined event keywords.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "As designed, part of the code pattern to author an event source.")]
        public static class Keywords
        {
            public const EventKeywords Sink = (EventKeywords)0x0001;
        }
    }
}
