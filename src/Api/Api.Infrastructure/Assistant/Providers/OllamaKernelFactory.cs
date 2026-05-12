#pragma warning disable SKEXP0070
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace Api.Infrastructure.Assistant.Providers;

public class OllamaKernelFactory(IConfiguration configuration) : IKernelFactory
{
    public Kernel CreateKernel()
    {
        string endpoint = configuration["Assistant:Ollama:Endpoint"] ?? "http://localhost:11434";
        string model = configuration["Assistant:Ollama:Model"] ?? "llama3.2";

        return Kernel.CreateBuilder()
            .AddOllamaChatCompletion(model, new Uri(endpoint))
            .Build();
    }
}
#pragma warning restore SKEXP0070
