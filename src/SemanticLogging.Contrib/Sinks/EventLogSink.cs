using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using SemanticLogging.Contrib.Utility;

namespace SemanticLogging.Contrib.Sinks
{
    /// <summary>
    /// A sink that writes to the EventLog.
    /// </summary>    
    /// <remarks>This class is thread-safe.</remarks>
    public class EventLogSink : IObserver<EventEntry>, IDisposable
    {
        private readonly IEventTextFormatter _formatter;
        private readonly bool _isAsync;
        private readonly object _lockObject = new object();
        private readonly object _flushLockObject = new object();
        private readonly EventLog _writer;
        private bool _disposed;
        private readonly BlockingCollection<EventEntry> _pendingEntries;
        private volatile TaskCompletionSource<bool> _flushSource = new TaskCompletionSource<bool>();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _asyncProcessorTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogSink" /> class.
        /// </summary>
        /// <param name="logName">Name of the EventLog.</param>
        /// <param name="formatter">The formatter for entries</param>
        /// <param name="isAsync">Specifies if the writing should be done asynchronously, or synchronously with a blocking call.</param>
        public EventLogSink(string logName, IEventTextFormatter formatter, bool isAsync) 
            : this(logName, "", formatter, isAsync)
        {
            
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogSink" /> class.
        /// </summary>
        /// <param name="logName">Name of the EventLog.</param>
        /// <param name="source">Eventlog source</param>
        /// <param name="formatter">The formatter for entries</param>
        /// <param name="isAsync">Specifies if the writing should be done asynchronously, or synchronously with a blocking call.</param>
        public EventLogSink(string logName, string source, IEventTextFormatter formatter, bool isAsync)
        {
            Guard.ArgumentNotNullOrEmpty(logName, nameof(logName));
            Guard.ArgumentNotNullOrEmpty(logName, nameof(source));
            Guard.ArgumentNotNull(formatter, nameof(formatter));

            _formatter = formatter;
            _writer = new EventLog(logName, ".", source);

            _isAsync = isAsync;

            _flushSource.SetResult(true);

            if (isAsync)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _pendingEntries = new BlockingCollection<EventEntry>();
                _asyncProcessorTask = Task.Factory.StartNew(WriteEntries, TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="EventLogSink"/> class.
        /// </summary>
        ~EventLogSink()
        {
            Dispose(false);
        }

        /// <summary>
        /// Flushes the buffer content to the file.
        /// </summary>
        /// <returns>The Task that gets completed when the buffer is flushed.</returns>
        public Task FlushAsync()
        {
            lock (_flushLockObject)
            {
                return _flushSource.Task;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">A value indicating whether or not the class is disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    lock (_lockObject)
                    {
                        if (!_disposed)
                        {
                            _disposed = true;

                            if (_isAsync)
                            {
                                _cancellationTokenSource.Cancel();
                                _asyncProcessorTask.Wait();
                                _pendingEntries.Dispose();
                                _cancellationTokenSource.Dispose();
                            }

                            _writer.Dispose();
                        }
                    }
                }
            }
        }

        private void OnSingleEventWritten(EventEntry entry)
        {
            var formattedEntry = entry.TryFormatAsString(_formatter);

            if (formattedEntry != null)
            {
                try
                {
                    lock (_lockObject)
                    {
                        _writer.WriteEntry(formattedEntry, entry.Schema.Level.ToEventLogLevel(), entry.EventId);
                    }
                }
                catch (Exception e)
                {
                    SemanticLoggingContribEventSource.Log.EventLogSinkWriteFailed(e.ToString());
                }
            }
        }

        private void WriteEntries()
        {
            var token = _cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                EventEntry entry;
                try
                {
                    if (_pendingEntries.Count == 0 && !_flushSource.Task.IsCompleted && !token.IsCancellationRequested)
                    {
                        lock (_flushLockObject)
                        {
                            if (_pendingEntries.Count == 0 && !_flushSource.Task.IsCompleted && !token.IsCancellationRequested)
                            {
                                _flushSource.TrySetResult(true);
                            }
                        }
                    }

                    entry = _pendingEntries.Take(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var formattedEntry = entry.TryFormatAsString(_formatter);
                if (formattedEntry != null)
                {
                    try
                    {
                        _writer.WriteEntry(formattedEntry, entry.Schema.Level.ToEventLogLevel(), entry.EventId);
                    }
                    catch (Exception e)
                    {
                        SemanticLoggingContribEventSource.Log.EventLogSinkWriteFailed(e.ToString());
                    }
                }
            }

            lock (_flushLockObject)
            {
                _flushSource.TrySetResult(true);
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="EventLogSink"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            FlushAsync().Wait();
            Dispose();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
            FlushAsync().Wait();
            Dispose();
        }

        /// <summary>
        /// Provides the sink with new data to write.
        /// </summary>
        /// <param name="value">The current entry to write to the file.</param>
        public void OnNext(EventEntry value)
        {
            if (_isAsync)
            {
                _pendingEntries.Add(value);

                if (_flushSource.Task.IsCompleted)
                {
                    lock (_flushLockObject)
                    {
                        if (_flushSource.Task.IsCompleted)
                        {
                            _flushSource = new TaskCompletionSource<bool>();
                        }
                    }
                }
            }
            else
            {
                OnSingleEventWritten(value);
            }
        }
    }
}
