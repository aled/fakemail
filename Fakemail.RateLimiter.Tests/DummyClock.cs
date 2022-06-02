using System;

namespace Fakemail.RateLimiter.Tests
{
    internal class DummyClock : IClock
    {
        private DateTime _value;

        public DummyClock(DateTime value)
        {
            _value = value;
        }

        public void Advance(TimeSpan timeSpan)
        {
            _value = _value.Add(timeSpan);
        }

        public DateTime UtcNow => _value;
    }
}
