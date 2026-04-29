using MecaManage.Application.Common.Models;

namespace MecaManage.Application.Common.Interfaces;

public interface IIAService
{
    Task<IADiagnosisResponse?> GetDiagnosisAsync(
        string description,
        string brand,
        string model,
        int year,
        int mileage,
        string fuelType,
        CancellationToken cancellationToken = default,
        Guid? chefAtelierId = null,
        Guid? garageId = null);
}
