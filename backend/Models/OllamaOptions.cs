public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "phi4-mini";
    public int NumCtx { get; set; } = 2048;
}