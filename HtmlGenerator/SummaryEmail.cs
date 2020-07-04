using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlGenerator
{
    public class SummaryEmail
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Subject { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}
