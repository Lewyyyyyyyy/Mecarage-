namespace MecaManage.Domain.Enums;

public enum AppointmentStatus
{
    Pending,             // Client requested appointment
    Approved,            // Chef approved appointment
    Declined,            // Chef declined appointment
    InProgress,          // Work has started
    Completed,           // Work completed
    Cancelled            // Cancelled by either party
}

