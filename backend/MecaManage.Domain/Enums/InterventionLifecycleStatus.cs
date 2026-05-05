namespace MecaManage.Domain.Enums;

public enum InterventionLifecycleStatus
{
    Created,            // Appointment approved, intervention opened
    UnderExamination,   // Mechanic is examining
    ExaminationReviewed,// Chef reviewed examination
    InvoicePending,     // Invoice sent to client awaiting decision
    Approved,           // Client approved (proceed = true)
    Rejected,           // Client rejected (proceed = false)
    RepairInProgress,   // Repair task in progress
    RepairCompleted,    // Mechanic submitted repair to chef
    ReadyForPickup,     // Chef validated, client notified
    Closed              // Payment done, car picked up
}

