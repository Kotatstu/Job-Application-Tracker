public class EmailClassification
{
    public bool IsJobRelated { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? StatusSignal { get; set; }
}