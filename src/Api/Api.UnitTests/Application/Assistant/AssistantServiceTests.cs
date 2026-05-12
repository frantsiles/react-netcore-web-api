#pragma warning disable SKEXP0001
using Api.Infrastructure.Assistant;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace Api.UnitTests.Application.Assistant;

public class AssistantServiceTests
{
    private static Kernel BuildKernelWithMock(Mock<IChatCompletionService> chatServiceMock)
    {
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatServiceMock.Object);
        return builder.Build();
    }

    [Fact]
    public async Task AskStreamingAsync_WithValidQuestion_ShouldReturnTokens()
    {
        Mock<IChatCompletionService> chatServiceMock = new();
        chatServiceMock
            .Setup(s => s.GetStreamingChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings?>(),
                It.IsAny<Kernel?>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateStream("Hola", " mundo"));

        Mock<IKernelFactory> kernelFactoryMock = new();
        kernelFactoryMock.Setup(f => f.CreateKernel())
                         .Returns(BuildKernelWithMock(chatServiceMock));

        Mock<ISender> senderMock = new();
        AssistantService service = new(kernelFactoryMock.Object, senderMock.Object);

        List<string> chunks = [];
        await foreach (string chunk in service.AskStreamingAsync("¿Cuántos usuarios hay?", Guid.NewGuid()))
            chunks.Add(chunk);

        chunks.Should().HaveCount(2);
        chunks[0].Should().Be("Hola");
        chunks[1].Should().Be(" mundo");
    }

    [Fact]
    public async Task AskStreamingAsync_WithCancellation_ShouldStopStreaming()
    {
        using CancellationTokenSource cts = new();

        Mock<IChatCompletionService> chatServiceMock = new();
        chatServiceMock
            .Setup(s => s.GetStreamingChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings?>(),
                It.IsAny<Kernel?>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateInfiniteStream(cts.Token));

        Mock<IKernelFactory> kernelFactoryMock = new();
        kernelFactoryMock.Setup(f => f.CreateKernel())
                         .Returns(BuildKernelWithMock(chatServiceMock));

        Mock<ISender> senderMock = new();
        AssistantService service = new(kernelFactoryMock.Object, senderMock.Object);

        cts.CancelAfter(50);

        Func<Task> act = async () =>
        {
            await foreach (string _ in service.AskStreamingAsync("pregunta", Guid.NewGuid(), cts.Token))
            {
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static async IAsyncEnumerable<StreamingChatMessageContent> CreateStream(params string[] chunks)
    {
        foreach (string chunk in chunks)
        {
            await Task.Yield();
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, chunk);
        }
    }

    private static async IAsyncEnumerable<StreamingChatMessageContent> CreateInfiniteStream(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(10, ct);
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, "chunk");
        }
    }
}
#pragma warning restore SKEXP0001
