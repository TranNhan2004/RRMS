import { HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ClientCorrelationService } from '../services/client-correlation.service';

export const CLIENT_ACTION_ID = new HttpContextToken<string | null>(() => null);

export const clientCorrelationInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const correlation = inject(ClientCorrelationService);
  const actionId = request.context.get(CLIENT_ACTION_ID) ?? correlation.getClientActionId();
  let headers = request.headers.set('X-Client-Request-Id', correlation.createRequestId());
  if (actionId) {
    headers = headers.set('X-Client-Action-Id', actionId);
  }
  return next(request.clone({ headers }));
};
