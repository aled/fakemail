namespace Fakemail.RateLimiter
{
    public interface IClock
    {
        public DateTime UtcNow { get; }
    }
}