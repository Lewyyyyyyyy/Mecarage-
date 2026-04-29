import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserDto, AssignUserToGarageRequest, AssignUserToGarageResponse } from '../models/user.models';

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private readonly usersUrl = `${environment.apiBaseUrl}/users`;

  constructor(private readonly http: HttpClient) {}

  getAllUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(this.usersUrl);
  }

  assignUserToGarage(request: AssignUserToGarageRequest): Observable<AssignUserToGarageResponse> {
    return this.http.post<AssignUserToGarageResponse>(`${this.usersUrl}/assign-to-garage`, request);
  }
}

