public interface IJobEmailClassifier
{
    Task<EmailClassification?> ClassifyAsync(string subject, string body, CancellationToken ct = default);
}