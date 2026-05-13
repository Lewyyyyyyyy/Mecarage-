// Symptom Report Models
export interface SymptomReportDto {
  id: string;
  clientId: string;
  vehicleId: string;
  vehicleBrand: string;
  vehicleModel: string;
  symptomsDescription: string;
  aiPredictedIssue: string | null;
  aiConfidenceScore: number | null;
  aiRecommendations: string | null;
  chefFeedback: string | null;
  status: string;
  submittedAt: string;
  reviewedAt: string | null;
  availablePeriodStart: string | null;
  availablePeriodEnd: string | null;
  garageId: string | null;
}

export interface CreateSymptomReportDto {
  vehicleId: string;
  symptomsDescription: string;
  garageId?: string;
  chefAtelierId?: string;
}

export interface ChefInboxItemDto {
  id: string;
  clientId: string;
  clientName: string;
  vehicleId: string;
  vehicleInfo: string;
  symptomsDescription: string;
  aiPredictedIssue: string | null;
  aiConfidenceScore: number | null;
  aiRecommendations: string | null;
  submittedAt: string;
  status: string;
  chefFeedback: string | null;
  reviewedAt: string | null;
  availablePeriodStart: string | null;
  availablePeriodEnd: string | null;
}

export interface AddChefFeedbackDto {
  feedback: string;
  newStatus: string;
  availablePeriodStart?: string | null;
  availablePeriodEnd?: string | null;
}

// Appointment Models
export interface AppointmentDto {
  id: string;
  vehicleId: string;
  symptomReportId?: string | null;
  vehicleInfo: string;
  garageName: string;
  preferredDate: string;
  preferredTime: string;
  status: string;
  createdAt: string;
}

export interface CreateAppointmentDto {
  vehicleId: string;
  garageId: string;
  preferredDate: string;
  preferredTime: string;
  symptomReportId?: string;
  specialRequests?: string;
}

export interface PendingAppointmentDto {
  id: string;
  clientId: string;
  clientName: string;
  vehicleId: string;
  vehicleInfo: string;
  preferredDate: string;
  preferredTime: string;   // "HH:mm:ss" from C# TimeSpan
  symptomSummary: string | null;
  createdAt: string;
}

// All appointments for a garage (includes closed/past ones for traceability)
export interface GarageAppointmentDto {
  id: string;
  clientId: string;
  clientName: string;
  vehicleId: string;
  vehicleInfo: string;
  preferredDate: string;
  preferredTime: string;
  symptomSummary: string | null;
  createdAt: string;
  status: string;
  declineReason: string | null;
  approvedAt: string | null;
}

// Invoice Models
export interface InvoiceLineItemDto {
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface InvoiceDto {
  id: string;
  invoiceNumber: string;
  garageName: string;
  serviceFee: number;
  partsTotal: number;
  totalAmount: number;
  clientApproved: boolean;
  status: string;
  createdAt: string;
  lineItems?: InvoiceLineItemDto[];
}

export interface CreateInvoiceLineItemDto {
  sparePartId?: string;
  description: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateInvoiceDto {
  appointmentId: string;
  serviceFee: number;
  lineItems?: CreateInvoiceLineItemDto[];
}

// Repair Task Models
export interface MechanicTaskDto {
  id: string;
  taskTitle: string;
  description: string;
  clientName: string;
  vehicleInfo: string;
  status: string;
  assignedAt: string;
  startedAt: string | null;
  completedAt: string | null;
  estimatedMinutes: number | null;
  garageId: string;
  invoiceApproved: boolean;
  completionNotes: string | null;
}

export interface SelectedSparePartDto {
  sparePartId: string;
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface UpdateMechanicTaskDto {
  submitToChef: boolean;
  mechanicNotes?: string | null;
  fileUrl?: string | null;
  parts?: SelectedSparePartDto[];
}

export interface CreateRepairTaskDto {
  appointmentId: string;
  taskTitle: string;
  description: string;
  estimatedMinutes?: number;
  mechanicIds?: string[];
}

export interface UpdateRepairTaskStatusDto {
  newStatus: string;
  completionNotes?: string;
}

export interface ExaminationPartDto {
  name: string;
  quantity: number;
  estimatedPrice: number;
  sparePartId?: string;
}

export interface ReviewPartDto {
  sparePartId: string;
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface SubmitExaminationDto {
  examinationObservations: string;
  partsNeeded: ExaminationPartDto[];
  fileUrl?: string | null;
}

export interface PendingExaminationDto {
  repairTaskId: string;
  assignmentId: string;
  taskTitle: string;
  clientName: string;
  vehicleInfo: string;
  mechanicName: string;
  examinationObservations: string;
  partsNeeded: ExaminationPartDto[];
  examinationSubmittedAt: string;
  fileUrl?: string | null;
}

// All examinations for a garage (includes reviewed/closed ones for traceability)
export interface GarageExaminationDto {
  repairTaskId: string;
  assignmentId: string;
  taskTitle: string;
  clientName: string;
  vehicleInfo: string;
  mechanicName: string;
  examinationObservations: string;
  partsNeeded: ExaminationPartDto[];
  examinationSubmittedAt: string;
  examinationStatus: string;   // "Pending" | "Approved" | "DeclinedByChef"
  fileUrl?: string | null;
}

// Repair ready task (invoice approved by client, chef can manage repair)
export interface RepairReadyTaskDto {
  taskId: string;
  taskTitle: string;
  description: string;
  clientName: string;
  vehicleInfo: string;
  taskStatus: string;
  invoiceStatus: string;
  invoiceTotal: number;
  invoiceNumber: string;
  appointmentDate: string;
  assignedMechanics: string[];
  invoiceApprovedAt: string;
}

// Vehicle Models
export interface VehicleDto {
  id: string;
  brand: string;
  model: string;
  year: number;
  licensePlate: string;
}

// Spare Part / Stock Models
export interface SparePartDto {
  id: string;
  code: string;
  name: string;
  description: string;
  category: string;
  unitPrice: number;
  stockQuantity: number;
  reorderLevel: number;
  manufacturer: string | null;
  partNumber: string | null;
  isActive: boolean;
  isLowStock: boolean;
  lastRestocked: string;
}

export interface CreateSparePartDto {
  code: string;
  name: string;
  description: string;
  category: string;
  unitPrice: number;
  stockQuantity: number;
  reorderLevel: number;
  manufacturer?: string | null;
  partNumber?: string | null;
}

export interface UpdateSparePartDto {
  name: string;
  description: string;
  category: string;
  unitPrice: number;
  reorderLevel: number;
  manufacturer?: string | null;
  partNumber?: string | null;
  isActive: boolean;
}

// Notification Models
export interface ClientNotificationDto {
  id: string;
  title: string;
  message: string;
  notificationType: string;
  isRead: boolean;
  createdAt: string;
  symptomReportId: string | null;
  appointmentId: string | null;
  repairTaskId: string | null;
  invoiceId: string | null;
}

// Intervention Lifecycle Models
export interface InterventionSummaryDto {
  id: string;
  status: string;
  proceedWithIntervention: boolean | null;
  clientName: string;
  vehicleInfo: string;
  garageName: string;
  invoiceNumber: string | null;
  paymentAmount: number | null;
  paymentMethod: string | null;
  paymentDate: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface InterventionDetailDto {
  id: string;
  status: string;
  proceedWithIntervention: boolean | null;
  tenantId: string;
  garageId: string;
  clientId: string;
  vehicleId: string;
  appointmentId: string | null;
  symptomReportId: string | null;
  invoiceId: string | null;
  repairTaskId: string | null;
  clientName: string;
  clientEmail: string;
  vehicleInfo: string;
  garageName: string;
  examinationNotes: string | null;
  partsUsedJson: string | null;
  repairNotes: string | null;
  invoiceNumber: string | null;
  invoiceTotal: number | null;
  invoiceStatus: string | null;
  paymentAmount: number | null;
  paymentMethod: string | null;
  paymentDate: string | null;
  paidBy: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface RegisterPaymentDto {
  paymentAmount: number;
  paymentMethod: string;
}

