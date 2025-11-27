namespace API.Configuration
{
    public class ApiKeyEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
    }

    public class ApiKeyOptions
    {
        public List<ApiKeyEntry> Keys { get; set; } = new();
    }
}
