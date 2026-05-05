using MecaManage.Domain.Common;
using MecaManage.Domain.Enums;

namespace MecaManage.Domain.Entities;

/// <summary>
/// Unified lifecycle tracker for a complete repair intervention.
/// Created when an appointment is approved. Tracks every stage
/// from examination → invoice → approval → repair → payment.
/// This is a side tracker — the existing flow (Appointment / Invoice /
/// RepairTask) continues to work independently.
/// </summary>
public class Intervention : BaseEntity
{
    // ── Identity ──────────────────────────────────────────────────────────
    public Guid TenantId  { get; set; }
    public Guid GarageId  { get; set; }
    public Guid ClientId  { get; set; }
    public Guid VehicleId { get; set; }

    // ── Links to existing entities (nullable — set as flow progresses) ───
    public Guid? AppointmentId   { get; set; }
    public Guid? SymptomReportId { get; set; }
    public Guid? InvoiceId       { get; set; }
    public Guid? RepairTaskId    { get; set; }

    // ── Lifecycle status ──────────────────────────────────────────────────
    public InterventionLifecycleStatus Status { get; set; } = InterventionLifecycleStatus.Created;

    // null = not yet decided, true = client approved, false = client rejected
    public bool? ProceedWithIntervention { get; set; }

    // ── Examination (filled post-examination) ────────────────────────────
    public string? ExaminationNotes { get; set; }
    public string? PartsUsedJson   { get; set; }  // JSON blob from inspection

    // ── Repair ───────────────────────────────────────────────────────────
    public string? RepairNotes { get; set; }

    // ── Payment (filled by admin when client picks up the car) ──────────
    public decimal?  PaymentAmount { get; set; }
    public string?   PaymentMethod { get; set; }  // Cash / Card / Transfer
    public DateTime? PaymentDate   { get; set; }
    public string?   PaidBy        { get; set; }  // Admin name / id who registered it

    // ── Navigation properties (no back-collections on parent entities) ──
    public Tenant        Tenant        { get; set; } = null!;
    public Garage        Garage        { get; set; } = null!;
    public User          Client        { get; set; } = null!;
    public Vehicle       Vehicle       { get; set; } = null!;
    public Appointment?  Appointment   { get; set; }
    public SymptomReport? SymptomReport { get; set; }
    public Invoice?      Invoice       { get; set; }
}

