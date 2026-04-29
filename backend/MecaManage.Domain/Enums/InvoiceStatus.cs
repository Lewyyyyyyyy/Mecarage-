namespace MecaManage.Domain.Enums;

public enum InvoiceStatus
{
    Draft,               // Chef is still creating invoice
    AwaitingApproval,    // Sent to client for approval
    ClientApproved,      // Client approved and can proceed
    ClientRejected,      // Client rejected invoice
    Paid,                // Invoice has been paid
    Cancelled            // Invoice was cancelled
}

