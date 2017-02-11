using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Configuration;
using SemanticLogging.Contrib.Observable;
using SemanticLogging.Contrib.Utility;

namespace SemanticLogging.Contrib.Configuration
{
    /// <summary>
    /// Represents a flat file configuration element that can create an instance of a flat file sink.
    /// </summary>
    internal class EventLogSinkElement : ISinkElement
    {
        private readonly XName sinkName = XName.Get("eventLogSink", "urn:schemas.SemanticLogging.Contrib.etw.eventLogSink");

        /// <summary>
        /// Determines whether this instance can create the specified configuration element.
        /// </summary>
        /// <param name="element">The configuration element.</param>
        /// <returns>
        ///   <c>True</c> if this instance can create the specified element; otherwise, <c>false</c>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated with Guard class")]
        public bool CanCreateSink(XElement element)
        {
            Guard.ArgumentNotNull(element, "element");

            return element.Name == sinkName;
        }

        /// <summary>
        /// Creates the <see cref="IObserver{EventEntry}" /> instance for this sink.
        /// </summary>
        /// <param name="element">The configuration element.</param>
        /// <returns>
        /// The sink instance.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated with Guard class")]
        public IObserver<EventEntry> CreateSink(XElement element)
        {
            Guard.ArgumentNotNull(element, "element");

            var subject = new EventEntrySubject();
            subject.LogToEventLog((string)element.Attribute("logName"), (string)element.Attribute("source"), FormatterElementFactory.Get(element));
            return subject;
        }
    }
}
