using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace SemanticLogging.Contrib.Utility
{
    static class EventLevelEx
    {
        public static EventLogEntryType ToEventLogLevel(this EventLevel This)
        {
            switch (This)
            {
                case EventLevel.Critical:
                case EventLevel.Error:
                    return EventLogEntryType.Error;

                case EventLevel.Warning:
                    return EventLogEntryType.Warning;

                case EventLevel.LogAlways:
                case EventLevel.Informational:
                case EventLevel.Verbose:
                    return EventLogEntryType.Information;
                default:
                    throw new ArgumentOutOfRangeException(nameof(This), This, null);
            }
        }
    }
}
