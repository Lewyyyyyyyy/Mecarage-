export type UrgencyLevel = 'Faible' | 'Modere' | 'Urgent' | 'Critique';

export type InterventionStatus =
  | 'EnAttente'
  | 'Accepte'
  | 'EnCours'
  | 'EnAttentePieces'
  | 'Termine'
  | 'Refuse'
  | 'Annule';

export interface InterventionDto {
  id: string;
  tenantId: string;
  clientId: string;
  vehicleId: string;
  garageId: string;
  description: string;
  status: InterventionStatus | string;
  urgencyLevel: UrgencyLevel | string;
  appointmentDate?: string | null;
  createdAt: string;
}

export interface CreateInterventionRequest {
  vehicleId: string;
  garageId: string;
  description: string;
  urgencyLevel: UrgencyLevel;
  appointmentDate?: string | null;
}

export interface CreateInterventionResponse {
  message: string;
  interventionId?: string;
}

export interface UpdateInterventionStatusRequest {
  newStatus: InterventionStatus;
  notes?: string | null;
}

export interface AssignMecanicienRequest {
  mecanicienId: string;
}

export interface MessageResponse {
  message: string;
}

export interface IADiagnosisResponse {
  diagnosis: string;
  confidenceScore: number;
  recommendedWorkshop: string;
  urgencyLevel: string;
  estimatedCostRange: string;
  recommendedActions: string;
  ragSourcesUsed: number;
}

export interface DiagnoseInterventionResponse {
  success: boolean;
  message: string;
  diagnosis?: IADiagnosisResponse | null;
}

