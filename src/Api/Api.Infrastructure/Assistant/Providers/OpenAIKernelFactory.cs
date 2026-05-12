using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace Api.Infrastructure.Assistant.Providers;

public class OpenAIKernelFactory(IConfiguration configuration) : IKernelFactory
{
    public Kernel CreateKernel()
    {
        string apiKey = configuration["Assistant:OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Assistant:OpenAI:ApiKey not configured.");
        string model = configuration["Assistant:OpenAI:Model"] ?? "gpt-4o-mini";

        return Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(model, apiKey)
            .Build();
    }
}
