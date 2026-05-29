using System;
using System.Collections.Generic;
using System.Threading;
using Nomad.Core.Logger;

namespace Nomad.Logger.Tests
{
    internal sealed class RecordingSink : ILoggerSink
    {
        private readonly object _gate = new();
        private readonly ManualResetEventSlim _messageReceived = new(false);

        public List<string> Messages { get; } = new();
        public int ClearCount { get; private set; }
        public int FlushCount { get; private set; }
        public int DisposeCount { get; private set; }

        public void Print(string message)
        {
            lock (_gate)
            {
                Messages.Add(message);
            }
            _messageReceived.Set();
        }

        public void Clear()
        {
            ClearCount++;
            lock (_gate)
            {
                Messages.Clear();
            }
        }

        public void Flush()
        {
            FlushCount++;
        }

        public void Dispose()
        {
            DisposeCount++;
        }

        public bool WaitForMessages(int count, int timeoutMilliseconds = 1500)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
            while (DateTime.UtcNow < deadline)
            {
                lock (_gate)
                {
                    if (Messages.Count >= count)
                    {
                        return true;
                    }
                }
                _messageReceived.Wait(25);
                _messageReceived.Reset();
            }

            lock (_gate)
            {
                return Messages.Count >= count;
            }
        }
    }

    internal sealed class ThrowingSink : ILoggerSink
    {
        public void Print(string message)
        {
            throw new InvalidOperationException("sink failed");
        }

        public void Clear()
        {
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}
