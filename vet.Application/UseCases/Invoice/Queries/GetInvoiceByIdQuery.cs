using MediatR;
using vet.Domain.Ports;

using InvoiceEntity = vet.Domain.Entities.Invoice;

namespace vet.Application.UseCases.Invoice.Queries;

public record GetInvoiceByIdQuery(long Id)
    : IRequest<InvoiceDto>;

public class GetInvoiceByIdQueryHandler
    : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInvoiceByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceDto> Handle(
        GetInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var invoice = await _unitOfWork
            .Repository<InvoiceEntity>()
            .GetByIdAsync(request.Id);

        if (invoice is null)
            throw new KeyNotFoundException(
                "El comprobante no existe.");

        return InvoiceDto.FromEntity(invoice);
    }
}