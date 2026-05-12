namespace Api.Application.Assistant;

public interface IAssistantService
{
    IAsyncEnumerable<string> AskStreamingAsync(
        string question,
        Guid requesterId,
        CancellationToken ct = default);
}
