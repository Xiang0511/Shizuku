namespace Shizuku.ViewModels
{
    public class LogViewModel
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public string Properties { get; set; } // 這裡通常存 JSON 或 XML，可以用來做進階正規化解析
    }
}
