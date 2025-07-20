namespace HandheldCommander.Models
{
    public class FileSystemItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public string Icon { get; set; } // Unicode or font icon
    }
} 