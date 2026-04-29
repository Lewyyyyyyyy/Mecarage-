export interface GarageDto {
  id: string;
  tenantId: string;
  name: string;
  address: string;
  city: string;
  phone: string;
  latitude?: number | null;
  longitude?: number | null;
  isActive: boolean;
  adminId?: string;
  adminFirstName?: string;
  adminLastName?: string;
}

export interface CreateGarageRequest {
  name: string;
  address: string;
  city: string;
  phone: string;
  latitude?: number | null;
  longitude?: number | null;
  tenantId?: string; // Optional - used when SuperAdmin creates garage for a specific tenant
}

export interface CreateGarageResponse {
  message: string;
  garageId?: string;
}

export interface GarageInterventionDto {
  id: string;
  clientName: string;
  clientEmail: string;
  vehicleInfo: string;
  status: string;
  description?: string;
  createdAt: Date;
  updatedAt?: Date;
}

export interface GarageClientDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  vehicleCount: number;
  interventionCount: number;
}
