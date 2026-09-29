using System;

namespace ASPCoreWebMVC.Models
{
    public class UploadedImage
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty; // relative url e.g. /uploads/xxx.jpg
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
