namespace MecaManage.Domain.Enums;

public enum RepairTaskStatus
{
    Assigned,            // Mechanic has been assigned
    InProgress,          // Mechanic started work
    Fixed,               // Issue fixed, awaiting testing
    Tested,              // Testing completed
    Done,                // Work completed and approved
    OnHold,              // Work paused (waiting for parts, weather, etc)
    Cancelled            // Work cancelled
}

