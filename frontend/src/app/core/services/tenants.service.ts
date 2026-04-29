import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateTenantRequest,
  CreateTenantResponse,
  TenantDto,
} from '../models/tenant.models';

export interface UpdateTenantRequest {
  name: string;
  email: string;
  phone: string;
}

export type { TenantDto, CreateTenantRequest };

@Injectable({
  providedIn: 'root',
})
export class TenantsService {
  private readonly tenantsUrl = `${environment.apiBaseUrl}/tenants`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<TenantDto[]> {
    return this.http.get<TenantDto[]>(this.tenantsUrl);
  }

  getTenantById(id: string): Observable<TenantDto> {
    return this.http.get<TenantDto>(`${this.tenantsUrl}/${id}`);
  }

  create(payload: CreateTenantRequest): Observable<CreateTenantResponse> {
    return this.http.post<CreateTenantResponse>(this.tenantsUrl, payload);
  }

  createTenant(payload: CreateTenantRequest): Observable<CreateTenantResponse> {
    return this.create(payload);
  }

  updateTenant(id: string, payload: UpdateTenantRequest): Observable<any> {
    return this.http.put(`${this.tenantsUrl}/${id}`, payload);
  }

  deleteTenant(id: string): Observable<any> {
    return this.http.delete(`${this.tenantsUrl}/${id}`);
  }
}


