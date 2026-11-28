import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, defer, finalize, map, Observable, of, shareReplay, switchMap, tap, throwError } from 'rxjs';
import { CLIENT_ACTION_ID } from '../interceptors/client-correlation.interceptor';
import { ClientCorrelationService } from './client-correlation.service';

export interface RegistrationRequest {
  stage: string;
  email?: string;
  token?: string;
  password?: string;
}

export interface RegistrationResponse {
  state: string;
  completionToken?: string;
}

export interface SessionResponse {
  reference: string;
  activeRole?: string;
  issuedAt?: string;
  expiresAt?: string;
}

export interface CurrentUserProfile {
  nationalIdNumber?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  birthDate?: string | null;
  sex?: string | null;
  addressUpdatedAt?: string | null;
}

export interface CurrentUser {
  id: string;
  userName: string | null;
  email: string;
  profile: CurrentUserProfile | null;
}

export interface AuthenticationResponse {
  state: string;
  userId?: string;
  userName?: string;
  activeRole?: string;
  eligibleRoles?: string[];
  pendingExpiresAt?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthenticationService {
  private readonly http = inject(HttpClient);
  private readonly clientCorrelationService = inject(ClientCorrelationService);
  private csrfReady$: Observable<void> = of(undefined);
  private readonly currentUserState = signal<CurrentUser | null>(null);
  readonly currentUser = this.currentUserState.asReadonly();

  initialize(): Observable<void> {
    return this.action(context => this.refreshCsrf(context).pipe(
      switchMap(() => this.http.get<CurrentUser>('/api/v1/users/me', { context })),
      tap(user => this.currentUserState.set(user)),
      map(() => undefined),
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          this.currentUserState.set(null);
          return of(undefined);
        }
        return throwError(() => error);
      }),
    ));
  }

  register(request: RegistrationRequest): Observable<RegistrationResponse> {
    return this.action(context => this.whenCsrfReady(() => this.http.post<RegistrationResponse>('/api/v1/auth/register', request, { context })));
  }

  verifyEmail(token: string): Observable<RegistrationResponse> {
    return this.action(context => this.whenCsrfReady(() => this.http.post<RegistrationResponse>('/api/v1/auth/verify-email', { token }, { context })));
  }

  beginGoogleLink(): Observable<string> {
    return this.action(context => this.whenCsrfReady(() => this.http.post<{ authorizationUrl: string }>('/api/v1/auth/google/link', {}, { context })).pipe(
      map(response => response.authorizationUrl),
    ));
  }

  getSessions(): Observable<SessionResponse[]> {
    return this.action(context => this.http.get<SessionResponse[]>('/api/v1/auth/sessions', { context }));
  }

  revokeSession(reference: string): Observable<void> {
    return this.action(context => this.whenCsrfReady(() => this.http.delete<void>(`/api/v1/auth/sessions/${encodeURIComponent(reference)}`, { context })));
  }

  login(identity: string, password: string): Observable<AuthenticationResponse> {
    return this.action(context => this.whenCsrfReady(() => this.http.post<AuthenticationResponse>('/api/v1/auth/login', { identity, password }, { context })).pipe(
      switchMap(result => result.state === 'AUTHENTICATED'
        ? this.refreshCsrf(context).pipe(
            switchMap(() => this.loadCurrentUser(context)),
            map(() => result),
          )
        : of(result).pipe(tap(() => this.currentUserState.set(null)))),
    ));
  }

  getPendingSession(): Observable<AuthenticationResponse> {
    return this.action(context => this.http.get<AuthenticationResponse>('/api/v1/auth/pending-session', { context }));
  }

  selectRole(roleCode: string): Observable<AuthenticationResponse> {
    return this.action(context => this.whenCsrfReady(() => this.http.post<AuthenticationResponse>('/api/v1/auth/select-role', { roleCode }, { context })).pipe(
      switchMap(result => this.refreshCsrf(context).pipe(
        switchMap(() => this.loadCurrentUser(context)),
        map(() => result),
      )),
    ));
  }

  switchRole(roleCode: string): Observable<void> {
    return this.action(context => this.whenCsrfReady(() => this.http.post<void>('/api/v1/auth/switch-role', { roleCode }, { context })).pipe(
      switchMap(() => this.refreshCsrf(context)),
      switchMap(() => this.loadCurrentUser(context)),
      map(() => undefined),
    ));
  }

  logout(allSessions = false): Observable<void> {
    const endpoint = allSessions ? 'logout-all' : 'logout';
    return this.action(context => this.whenCsrfReady(() => this.http.post<void>(`/api/v1/auth/${endpoint}`, {}, { context })).pipe(
      tap(() => this.currentUserState.set(null)),
      switchMap(() => this.refreshCsrf(context)),
    ));
  }

  private loadCurrentUser(context: HttpContext): Observable<CurrentUser> {
    return this.http.get<CurrentUser>('/api/v1/users/me', { context }).pipe(
      tap(user => this.currentUserState.set(user)),
    );
  }

  private refreshCsrf(context: HttpContext): Observable<void> {
    const refresh = this.http.get<void>('/api/v1/csrf', { context }).pipe(
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    this.csrfReady$ = refresh;
    return refresh;
  }

  private whenCsrfReady<T>(operation: () => Observable<T>): Observable<T> {
    return this.csrfReady$.pipe(switchMap(() => operation()));
  }

  private action<T>(operation: (context: HttpContext) => Observable<T>): Observable<T> {
    return defer(() => {
      const actionId = this.clientCorrelationService.startAction();
      const context = new HttpContext().set(CLIENT_ACTION_ID, actionId);
      return operation(context).pipe(finalize(() => this.clientCorrelationService.completeAction(actionId)));
    });
  }
}
