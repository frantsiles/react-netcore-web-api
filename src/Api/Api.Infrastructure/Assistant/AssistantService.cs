#pragma warning disable SKEXP0001
using Api.Application.Assistant;
using Api.Application.Assistant.Plugins;
using MediatR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Runtime.CompilerServices;

namespace Api.Infrastructure.Assistant;

public class AssistantService(IKernelFactory kernelFactory, ISender sender) : IAssistantService
{
    private const string SystemPrompt =
        """
        Eres un asistente administrativo del sistema react-netcore-web-api.
        Tienes acceso a herramientas para gestionar usuarios y sesiones.

        Capacidades disponibles:
        - Consultar y crear usuarios del sistema
        - Ver sesiones activas y revocarlas
        - Responder preguntas sobre el estado del sistema

        Instrucciones:
        - Responde siempre en el mismo idioma en que te hablen
        - Usa las herramientas disponibles para responder con datos reales
        - Sé conciso pero informativo
        - Cuando crees o modifiques datos, confirma la acción realizada
        - Si una operación requiere permisos de Admin, indícalo claramente
        - Nunca inventes datos — si no tienes acceso a algo, dilo
        """;

    public async IAsyncEnumerable<string> AskStreamingAsync(
        string question,
        Guid requesterId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        Kernel kernel = kernelFactory.CreateKernel();
        kernel.Plugins.AddFromObject(new UserPlugin(sender), "Users");
        kernel.Plugins.AddFromObject(new SessionPlugin(sender), "Sessions");

        OpenAIPromptExecutionSettings settings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.3,
            MaxTokens = 1000
        };

        ChatHistory chatHistory = new(SystemPrompt);
        chatHistory.AddUserMessage(question);

        IChatCompletionService chatService =
            kernel.GetRequiredService<IChatCompletionService>();

        await foreach (StreamingChatMessageContent content in
            chatService.GetStreamingChatMessageContentsAsync(chatHistory, settings, kernel, ct))
        {
            if (!string.IsNullOrEmpty(content.Content))
                yield return content.Content;
        }
    }
}
#pragma warning restore SKEXP0001
