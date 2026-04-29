export interface TenantDto {
  id: string;
  name: string;
  slug: string;
  email: string;
  phone: string;
  isActive: boolean;
  createdAt: string;
  garageCount?: number;
  userCount?: number;
}

export interface CreateTenantRequest {
  name: string;
  slug: string;
  email: string;
  phone: string;
}

export interface CreateTenantResponse {
  message: string;
  tenantId?: string;
}

