namespace MecaManage.Domain.Enums;

public enum SymptomReportStatus
{
    Submitted,           // Client submitted symptoms
    PendingReview,       // Awaiting Chef feedback
    Reviewed,            // Chef has reviewed and added feedback
    Approved,            // Approved for appointment booking
    Declined,            // Chef declined - client needs to resubmit
    Archived             // Old reports
}

