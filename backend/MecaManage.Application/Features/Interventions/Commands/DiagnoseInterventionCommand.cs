using MecaManage.Application.Common.Interfaces;
using MecaManage.Application.Common.Models;
using MecaManage.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.Interventions.Commands;

public record DiagnoseInterventionCommand(Guid InterventionId) : IRequest<DiagnoseInterventionResult>;

public record DiagnoseInterventionResult(bool Success, string Message, IADiagnosisResponse? Diagnosis = null);

public class DiagnoseInterventionCommandHandler : IRequestHandler<DiagnoseInterventionCommand, DiagnoseInterventionResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IIAService _iaService;

    public DiagnoseInterventionCommandHandler(IApplicationDbContext context, IIAService iaService)
    {
        _context = context;
        _iaService = iaService;
    }

    public async Task<DiagnoseInterventionResult> Handle(DiagnoseInterventionCommand request, CancellationToken cancellationToken)
    {
        var intervention = await _context.InterventionRequests
            .Include(i => i.Vehicle)
            .FirstOrDefaultAsync(i => i.Id == request.InterventionId, cancellationToken);

        if (intervention == null)
            return new DiagnoseInterventionResult(false, "Intervention introuvable");

        var diagnosis = await _iaService.GetDiagnosisAsync(
            intervention.Description,
            intervention.Vehicle.Brand,
            intervention.Vehicle.Model,
            intervention.Vehicle.Year,
            intervention.Vehicle.Mileage,
            intervention.Vehicle.FuelType,
            cancellationToken);

        if (diagnosis == null)
            return new DiagnoseInterventionResult(false, "Le service IA n'a pas pu générer un diagnostic");

        var aiDiagnosis = new AIDiagnosis
        {
            InterventionRequestId = intervention.Id,
            Diagnosis = diagnosis.Diagnosis,
            ConfidenceScore = diagnosis.ConfidenceScore,
            RecommendedWorkshop = diagnosis.RecommendedWorkshop,
            UrgencyLevel = diagnosis.UrgencyLevel,
            EstimatedCostRange = diagnosis.EstimatedCostRange,
            RecommendedActions = diagnosis.RecommendedActions,
            RagSourcesUsed = diagnosis.RagSourcesUsed
        };

        _context.AIDiagnoses.Add(aiDiagnosis);
        intervention.DiagnosisResult = diagnosis.Diagnosis;
        await _context.SaveChangesAsync(cancellationToken);

        return new DiagnoseInterventionResult(true, "Diagnostic généré avec succès", diagnosis);
    }
}
