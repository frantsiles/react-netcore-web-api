using Microsoft.SemanticKernel;

namespace Api.Infrastructure.Assistant;

public interface IKernelFactory
{
    Kernel CreateKernel();
}
