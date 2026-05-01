namespace Shizuku.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }  // 是否成功
        public required string Message { get; set; } // 提示訊息 
        public T? Data { get; set; }        // 實際資料內容 
    }
}
