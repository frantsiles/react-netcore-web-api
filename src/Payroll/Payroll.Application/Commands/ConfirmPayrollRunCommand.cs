using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Services;
using static Payroll.Application.Commands.CreatePayrollRunHandler;

namespace Payroll.Application.Commands;

public record ConfirmPayrollRunCommand(Guid RunId) : IRequest<PayrollRunDto>;

public class ConfirmPayrollRunHandler(IPayrollRunRepository repo)
    : IRequestHandler<ConfirmPayrollRunCommand, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(ConfirmPayrollRunCommand req, CancellationToken ct)
    {
        var run = await repo.GetByIdAsync(req.RunId, ct)
            ?? throw new InvalidOperationException($"PayrollRun {req.RunId} not found.");
        run.Confirm();
        await repo.SaveChangesAsync(ct);
        return CreatePayrollRunHandler.ToDto(run);
    }
}
