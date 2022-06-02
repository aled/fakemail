namespace Fakemail.RateLimiter
{
    public class SystemClock : IClock
    {
        public DateTime UtcNow
        {
            get
            { 
                return DateTime.UtcNow;
            }
        }
    }
}