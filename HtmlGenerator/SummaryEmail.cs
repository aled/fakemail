using System;

namespace HtmlGenerator
{
    public record SummaryEmail
    {
        public string Id;
        public DateTime Timestamp;
        public string Subject;
        public string From;
        public string To;
    }
}
