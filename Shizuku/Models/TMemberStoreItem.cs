namespace Shizuku.Models
{
    public class TMemberStoreItem
    {
        public int FId { get; set; }
        public string FItemId { get; set; } = null!;
        public string FItemName { get; set; } = null!;
        public int FPointsRequired { get; set; }
        public string FImgPath { get; set; } = null!;
        public int FStock { get; set; }
    }
}
