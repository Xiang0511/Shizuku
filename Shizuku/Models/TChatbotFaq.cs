namespace Shizuku.Models
{
    public class TChatbotFaq
    {
        public int fId { get; set; }
        public string fKeyword { get; set; } = null!;
        public string fAnswer { get; set; } = null!;
    }
}