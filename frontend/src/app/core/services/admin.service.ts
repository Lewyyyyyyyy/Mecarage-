import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminKpis } from '../models/admin.models';
import {
  CreateGarageAdminRequest,
  CreateGarageAdminResponse,
  UpdateGarageAdminRequest,
  UpdateGarageAdminResponse,
  GetGarageAdminsResponse
} from '../models/garage-admin.models';

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  private readonly adminUrl = `${environment.apiBaseUrl}/admin`;

  constructor(private readonly http: HttpClient) {}

  getKpis(): Observable<AdminKpis> {
    return this.http.get<AdminKpis>(`${this.adminUrl}/kpis`);
  }

  createGarageAdmin(request: CreateGarageAdminRequest): Observable<CreateGarageAdminResponse> {
    return this.http.post<CreateGarageAdminResponse>(
      `${this.adminUrl}/create-garage-admin`,
      request
    );
  }

  getGarageAdmins(tenantId: string): Observable<GetGarageAdminsResponse> {
    return this.http.get<GetGarageAdminsResponse>(
      `${this.adminUrl}/garage-admins/${tenantId}`
    );
  }

  updateGarageAdmin(garageId: string, request: UpdateGarageAdminRequest): Observable<UpdateGarageAdminResponse> {
    return this.http.put<UpdateGarageAdminResponse>(
      `${this.adminUrl}/garage-admin/${garageId}`,
      request
    );
  }
}

