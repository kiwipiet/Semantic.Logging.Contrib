using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using SemanticLogging.Contrib.Sinks;

namespace SemanticLogging.Contrib
{
    /// <summary>
    /// Factories and helpers for using the <see cref="EventLogSink"/>.
    /// </summary>
    public static class EventLogLog
    {
        /// <summary>
        /// Subscribes to an <see cref="IObservable{EventEntry}"/> using a <see cref="EventLogSink"/>.
        /// </summary>
        /// <param name="eventStream">The event stream. Typically this is an instance of <see cref="ObservableEventListener"/>.</param>
        /// <param name="logName">Name of the EventLog</param>
        /// <param name="source">Eventlog source</param>
        /// <param name="formatter">The formatter.</param>
        /// <param name="isAsync">Specifies if the writing should be done asynchronously, or synchronously with a blocking call.</param>
        /// <returns>A subscription to the sink that can be disposed to unsubscribe the sink and dispose it, or to get access to the sink instance.</returns>
        public static SinkSubscription<EventLogSink> LogToEventLog(this IObservable<EventEntry> eventStream, string logName = "Application", string source = "", IEventTextFormatter formatter = null, bool isAsync = false)
        {
            if (string.IsNullOrEmpty(source))
            {
                source = "";
            }

            var sink = new EventLogSink(logName, source, formatter ?? new EventTextFormatter(), isAsync);

            var subscription = eventStream.Subscribe(sink);

            return new SinkSubscription<EventLogSink>(subscription, sink);
        }

        /// <summary>
        /// Creates an event listener that logs using a <see cref="EventLogSink"/>.
        /// </summary>
        /// <param name="logName">Name of the EventLog</param>
        /// <param name="source">Eventlog source</param>
        /// <param name="formatter">The formatter.</param>
        /// <param name="isAsync">Specifies if the writing should be done asynchronously, or synchronously with a blocking call.</param>
        /// <returns>An event listener that uses <see cref="EventLogSink"/> to log events.</returns>
        public static EventListener CreateListener(string logName = "Application", string source = "", IEventTextFormatter formatter = null, bool isAsync = false)
        {
            var listener = new ObservableEventListener();
            listener.LogToEventLog(logName, source, formatter, isAsync);
            return listener;
        }
    }
}
