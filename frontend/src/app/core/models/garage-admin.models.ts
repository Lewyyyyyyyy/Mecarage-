export interface GarageAdminDto {
  garageId: string;
  garageName: string;
  adminId?: string;
  adminFirstName?: string;
  adminLastName?: string;
  adminEmail?: string;
  adminPhone?: string;
  hasAdmin: boolean;
}

export interface CreateGarageAdminRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phone: string;
  tenantId: string;
  garageId: string;
}

export interface CreateGarageAdminResponse {
  message: string;
  adminId?: string;
}

export interface UpdateGarageAdminRequest {
  newAdminId?: string;
}

export interface UpdateGarageAdminResponse {
  message: string;
}

export interface GetGarageAdminsResponse {
  success: boolean;
  message: string;
  data: GarageAdminDto[];
}

