using MecaManage.Application.Common.Interfaces;
using MecaManage.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MecaManage.Application.Features.RepairTasks.Commands;

public record MarkTaskTestedCommand(
    Guid TaskId,
    Guid ChefId
) : IRequest<MarkTaskTestedResult>;

public record MarkTaskTestedResult(bool Success, string Message);

public class MarkTaskTestedCommandHandler : IRequestHandler<MarkTaskTestedCommand, MarkTaskTestedResult>
{
    private readonly IApplicationDbContext _context;

    public MarkTaskTestedCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MarkTaskTestedResult> Handle(MarkTaskTestedCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.RepairTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.AssignedByChefId == request.ChefId, cancellationToken);

        if (task == null)
            return new MarkTaskTestedResult(false, "Tâche introuvable ou accès refusé");

        if (task.Status != RepairTaskStatus.Fixed)
            return new MarkTaskTestedResult(false, "La tâche doit être marquée comme réparée (Fixed) avant de pouvoir être testée");

        task.Status = RepairTaskStatus.Tested;
        _context.RepairTasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);

        return new MarkTaskTestedResult(true, "Tâche marquée comme testée. Vous pouvez maintenant signaler la disponibilité au client.");
    }
}

