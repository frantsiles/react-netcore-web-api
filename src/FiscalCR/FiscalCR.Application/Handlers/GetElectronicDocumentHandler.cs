using FiscalCR.Application.Commands;
using FiscalCR.Application.DTOs;
using FiscalCR.Domain.ElectronicDocuments;
using FiscalCR.Domain.Services;
using MediatR;

namespace FiscalCR.Application.Handlers;

public class GetElectronicDocumentHandler(IElectronicDocumentRepository docRepo)
    : IRequestHandler<GetElectronicDocumentQuery, ElectronicDocumentDto?>
{
    public async Task<ElectronicDocumentDto?> Handle(GetElectronicDocumentQuery request, CancellationToken ct)
    {
        var doc = await docRepo.FindByInvoiceIdAsync(request.InvoiceId, ct);
        if (doc is null) return null;
        return new ElectronicDocumentDto(
            doc.Id, doc.InvoiceId, doc.InvoiceNumber, doc.Clave, doc.NumeroConsecutivo,
            doc.Status.ToString(), doc.HaciendaEstado, doc.HaciendaMensaje,
            doc.CreatedAt, doc.SubmittedAt, doc.AcknowledgedAt);
    }
}

public class PollDocumentStatusHandler(
    IElectronicDocumentRepository docRepo,
    IHaciendaClient haciendaClient)
    : IRequestHandler<PollDocumentStatusCommand, ElectronicDocumentDto>
{
    public async Task<ElectronicDocumentDto> Handle(PollDocumentStatusCommand request, CancellationToken ct)
    {
        var doc = await docRepo.FindByInvoiceIdAsync(request.InvoiceId, ct)
            ?? throw new InvalidOperationException($"No electronic document found for invoice {request.InvoiceId}");

        if (doc.Status is ElectronicDocumentStatus.Accepted or ElectronicDocumentStatus.Rejected)
            return ToDto(doc); // already final

        var status = await haciendaClient.QueryStatusAsync(doc.Clave, doc.TenantId, ct);
        if (status is not null)
        {
            if (status.IndEstado == "aceptado")
                doc.MarkAccepted(status.Mensaje ?? "Aceptado por Hacienda", status.XmlRespuesta);
            else if (status.IndEstado == "rechazado")
                doc.MarkRejected(status.Mensaje ?? "Rechazado por Hacienda", status.XmlRespuesta);
        }

        await docRepo.SaveChangesAsync(ct);
        return ToDto(doc);
    }

    private static ElectronicDocumentDto ToDto(ElectronicDocument doc) => new(
        doc.Id, doc.InvoiceId, doc.InvoiceNumber, doc.Clave, doc.NumeroConsecutivo,
        doc.Status.ToString(), doc.HaciendaEstado, doc.HaciendaMensaje,
        doc.CreatedAt, doc.SubmittedAt, doc.AcknowledgedAt);
}
