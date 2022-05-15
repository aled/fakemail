namespace Fakemail.ApiModels
{
    public class SmtpUserDetail
    {
        public string SmtpUsername { get; set; }

        public string SmtpPassword { get; set; }

        public int CurrentEmailCount { get; set; }
    }
}
