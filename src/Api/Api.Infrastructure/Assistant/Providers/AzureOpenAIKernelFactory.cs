using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace Api.Infrastructure.Assistant.Providers;

public class AzureOpenAIKernelFactory(IConfiguration configuration) : IKernelFactory
{
    public Kernel CreateKernel()
    {
        string endpoint = configuration["Assistant:AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("Assistant:AzureOpenAI:Endpoint not configured.");
        string deploymentName = configuration["Assistant:AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("Assistant:AzureOpenAI:DeploymentName not configured.");
        string apiKey = configuration["Assistant:AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Assistant:AzureOpenAI:ApiKey not configured.");

        return Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
            .Build();
    }
}
