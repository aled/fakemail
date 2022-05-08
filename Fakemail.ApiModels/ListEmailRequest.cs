namespace Fakemail.ApiModels
{
    public class ListEmailRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string SmtpUsername { get; set; }
    }
}
