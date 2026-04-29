import { HttpInterceptorFn } from '@angular/common/http';

const AUTH_ENDPOINTS = ['/auth/login', '/auth/register', '/auth/refresh'];

export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const isAuthCall = AUTH_ENDPOINTS.some((endpoint) => req.url.includes(endpoint));

  if (isAuthCall) {
    return next(req);
  }

  const accessToken = sessionStorage.getItem('accessToken');

  if (!accessToken) {
    return next(req);
  }

  const authenticatedRequest = req.clone({
    setHeaders: {
      Authorization: `Bearer ${accessToken}`,
    },
  });

  return next(authenticatedRequest);
};
