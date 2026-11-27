using MediatR;
using vet.Domain.Entities;
using vet.Domain.Ports;

namespace vet.Application.UseCases.Sale.Queries;

// ========================================
// QUERY
// ========================================

public record GetBillableServicesQuery(
    long OwnerId
) : IRequest<List<BillableServiceResponse>>;

// ========================================
// RESPONSE
// ========================================

public class BillableServiceResponse
{
    public long VisitServiceId { get; set; }

    public long VisitId { get; set; }

    public long AppointmentId { get; set; }

    public long OwnerId { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public long PetId { get; set; }

    public string PetName { get; set; } = string.Empty;

    public long ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public decimal FinalPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime AppointmentDate { get; set; }
}

// ========================================
// HANDLER
// ========================================

public class GetBillableServicesQueryHandler
    : IRequestHandler<
        GetBillableServicesQuery,
        List<BillableServiceResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBillableServicesQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<BillableServiceResponse>> Handle(
        GetBillableServicesQuery request,
        CancellationToken cancellationToken)
    {
        // ========================================
        // 1. VALIDAR PROPIETARIO
        // ========================================

        if (request.OwnerId <= 0)
        {
            throw new ArgumentException(
                "El propietario no es válido.");
        }

        var owner = await _unitOfWork
            .Repository<Domain.Entities.Owner>()
            .GetByIdAsync(request.OwnerId);

        if (owner is null)
        {
            throw new ArgumentException(
                "El propietario no existe.");
        }

        // ========================================
        // 2. OBTENER MASCOTAS DEL PROPIETARIO
        // ========================================

        var pets = await _unitOfWork
            .Repository<Domain.Entities.Pet>()
            .FindAsync(
                p => p.OwnerId == request.OwnerId);

        var petDictionary = pets.ToDictionary(p => p.Id);

        var petIds = petDictionary.Keys.ToList();

        if (petIds.Count == 0)
        {
            return new List<BillableServiceResponse>();
        }

        // ========================================
        // 3. OBTENER CITAS VÁLIDAS
        // ========================================

        var appointments = await _unitOfWork
            .Repository<Domain.Entities.Appointment>()
            .FindAsync(
                a => petIds.Contains(a.PetId)
                     && (
                         a.Status == "IN_PROGRESS"
                         || a.Status == "COMPLETED"
                     ));

        var appointmentDictionary = appointments
            .ToDictionary(a => a.Id);

        var appointmentIds = appointmentDictionary
            .Keys
            .ToList();

        if (appointmentIds.Count == 0)
        {
            return new List<BillableServiceResponse>();
        }

        // ========================================
        // 4. OBTENER ATENCIONES
        // ========================================

        var visits = await _unitOfWork
            .Repository<Visit>()
            .FindAsync(
                v => petIds.Contains(v.PetId)
                     && v.AppointmentId.HasValue
                     && appointmentIds.Contains(
                         v.AppointmentId.Value));

        var visitDictionary = visits
            .ToDictionary(v => v.Id);

        var visitIds = visitDictionary.Keys.ToList();

        if (visitIds.Count == 0)
        {
            return new List<BillableServiceResponse>();
        }

        // ========================================
        // 5. OBTENER SERVICIOS REALIZADOS
        // ========================================

        var visitServices = await _unitOfWork
            .Repository<Domain.Entities.VisitService>()
            .FindAsync(
                vs => visitIds.Contains(vs.VisitId)
                      && vs.Status == "COMPLETED");

        var completedServices = visitServices.ToList();

        var visitServiceIds = completedServices
            .Select(vs => vs.Id)
            .ToList();

        if (visitServiceIds.Count == 0)
        {
            return new List<BillableServiceResponse>();
        }

        // ========================================
        // 6. IDENTIFICAR SERVICIOS YA ASOCIADOS
        //    A UNA VENTA
        // ========================================

        var saleItems = await _unitOfWork
            .Repository<SaleItem>()
            .FindAsync(
                si => si.VisitServiceId.HasValue
                      && visitServiceIds.Contains(
                          si.VisitServiceId.Value));

        var assignedServiceIds = saleItems
            .Where(si => si.VisitServiceId.HasValue)
            .Select(si => si.VisitServiceId!.Value)
            .ToHashSet();

        // ========================================
        // 7. FILTRAR SERVICIOS DISPONIBLES
        // ========================================

        var billableServices = completedServices
            .Where(vs => !assignedServiceIds.Contains(vs.Id))
            .ToList();

        if (billableServices.Count == 0)
        {
            return new List<BillableServiceResponse>();
        }

        // ========================================
        // 8. OBTENER NOMBRES DE LOS SERVICIOS
        // ========================================

        var serviceIds = billableServices
            .Select(vs => vs.ServiceId)
            .Distinct()
            .ToList();

        var services = await _unitOfWork
            .Repository<Domain.Entities.Service>()
            .FindAsync(
                s => serviceIds.Contains(s.Id));

        var serviceDictionary = services
            .ToDictionary(s => s.Id);

        // ========================================
        // 9. CONSTRUIR RESPUESTA
        // ========================================

        var result = new List<BillableServiceResponse>();

        foreach (var visitService in billableServices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var visit = visitDictionary[
                visitService.VisitId];

            var appointment = appointmentDictionary[
                visit.AppointmentId!.Value];

            var pet = petDictionary[visit.PetId];

            var service = serviceDictionary[
                visitService.ServiceId];

            result.Add(new BillableServiceResponse
            {
                VisitServiceId = visitService.Id,

                VisitId = visit.Id,

                AppointmentId = appointment.Id,

                OwnerId = owner.Id,

                OwnerName = owner.FirstNames,

                PetId = pet.Id,

                PetName = pet.Name,

                ServiceId = service.Id,

                ServiceName = service.Name,

                FinalPrice = visitService.FinalPrice,

                Status = visitService.Status,

                AppointmentDate = appointment.StartAt
            });
        }

        return result
            .OrderByDescending(x => x.AppointmentDate)
            .ThenBy(x => x.VisitServiceId)
            .ToList();
    }
}