namespace Shizuku.DTOs
{
    public class MemberAddressDto
    {
        // 將這三個資訊打包進 JSON 
        public string fReceiverName { get; set; } = string.Empty;
        public string fReceiverPhone { get; set; } = string.Empty;

        // 地址部分可以再細分，方便前端做下拉選單
        public string fCity { get; set; } = string.Empty;
        public string fArea { get; set; } = string.Empty;
        public string fAddressDetail { get; set; } = string.Empty;
        public string fZipCode { get; set; } = string.Empty;

        public bool fIsDefault { get; set; }
    }
}
