import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateStaffDto, StaffDto, CreateStaffResponse } from '../models/staff.models';

@Injectable({
  providedIn: 'root',
})
export class StaffService {
  private readonly usersUrl = `${environment.apiBaseUrl}/users`;

  constructor(private readonly http: HttpClient) {}

  createStaff(data: CreateStaffDto): Observable<CreateStaffResponse> {
    return this.http.post<CreateStaffResponse>(`${this.usersUrl}/create-staff`, data);
  }

  getGarageStaff(garageId: string): Observable<StaffDto[]> {
    return this.http.get<StaffDto[]>(`${this.usersUrl}/garage/${garageId}/staff`);
  }

  deleteStaff(userId: string): Observable<any> {
    return this.http.delete(`${this.usersUrl}/${userId}`);
  }
}

