namespace Fakemail.ApiModels
{
    public class GetEmailResponse
    {
        public bool Success { get; set; } = false;

        public string ErrorMessage { get; set; } = null;

        public string Filename { get; set; } = null;

        public byte[] Bytes { get; set; } = null;
    }
}
