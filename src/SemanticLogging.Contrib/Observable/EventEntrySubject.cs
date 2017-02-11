using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using SemanticLogging.Contrib.Utility;

namespace SemanticLogging.Contrib.Observable
{
    /// <summary>
    /// A subject that can be observed and publish events.
    /// </summary>    
    /// <remarks>
    /// This is a very basic implementation of a subject to avoid references to Rx when the
    /// end user might not want to do advanced filtering and projection of event streams.
    /// </remarks>
    internal sealed class EventEntrySubject : IObservable<EventEntry>, IObserver<EventEntry>, IDisposable
    {
        private readonly object _lockObject = new object();
        private volatile ReadOnlyCollection<IObserver<EventEntry>> _observers = new List<IObserver<EventEntry>>().AsReadOnly();
        private volatile bool _isFrozen;

        /// <summary>
        /// Releases all resources used by the current instance and unsubscribes all the observers.
        /// </summary>
        public void Dispose()
        {
            OnCompleted();
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications
        /// before the provider has finished sending them.</returns>
        public IDisposable Subscribe(IObserver<EventEntry> observer)
        {
            Guard.ArgumentNotNull(observer, "observer");

            lock (_lockObject)
            {
                if (!_isFrozen)
                {
                    var copy = _observers.ToList();
                    copy.Add(observer);
                    _observers = copy.AsReadOnly();
                    return new Subscription(this, observer);
                }
            }

            observer.OnCompleted();
            return new EmptyDisposable();
        }

        private void Unsubscribe(IObserver<EventEntry> observer)
        {
            lock (_lockObject)
            {
                _observers = _observers.Where(x => !observer.Equals(x)).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            var currentObservers = TakeObserversAndFreeze();

            if (currentObservers != null)
            {
                Parallel.ForEach(currentObservers, observer => observer.OnCompleted());
            }
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
            var currentObservers = TakeObserversAndFreeze();

            if (currentObservers != null)
            {
                Parallel.ForEach(currentObservers, observer => observer.OnError(error));
            }
        }

        /// <summary>
        /// Provides the observers with new data.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        public void OnNext(EventEntry value)
        {
            foreach (var observer in _observers)
            {
                // TODO: should I isolate errors (i.e: try/catch around each OnNext call)?
                observer.OnNext(value);
            }
        }

        private ReadOnlyCollection<IObserver<EventEntry>> TakeObserversAndFreeze()
        {
            lock (_lockObject)
            {
                if (!_isFrozen)
                {
                    _isFrozen = true;
                    var copy = _observers;
                    _observers = new List<IObserver<EventEntry>>().AsReadOnly();

                    return copy;
                }

                return null;
            }
        }

        private sealed class Subscription : IDisposable
        {
            private IObserver<EventEntry> _observer;
            private EventEntrySubject _subject;

            public Subscription(EventEntrySubject subject, IObserver<EventEntry> observer)
            {
                _subject = subject;
                _observer = observer;
            }

            public void Dispose()
            {
                var current = Interlocked.Exchange(ref _observer, null);
                if (current != null)
                {
                    _subject.Unsubscribe(current);
                    _subject = null;
                }
            }
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
