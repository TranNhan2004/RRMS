import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { clientCorrelationInterceptor } from '../interceptors/client-correlation.interceptor';
import { AuthenticationService } from './authentication.service';
import { ClientCorrelationService } from './client-correlation.service';

describe('AuthenticationService', () => {
  let service: AuthenticationService;
  let requests: HttpTestingController;
  let correlation: ClientCorrelationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([clientCorrelationInterceptor])), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthenticationService);
    requests = TestBed.inject(HttpTestingController);
    correlation = TestBed.inject(ClientCorrelationService);
  });

  afterEach(() => requests.verify());

  it('initializes CSRF before resolving an anonymous session', () => {
    let completed = false;
    service.initialize().subscribe(() => completed = true);
    const csrf = requests.expectOne('/api/v1/csrf');
    const actionId = csrf.request.headers.get('X-Client-Action-Id');
    csrf.flush(null);
    const me = requests.expectOne('/api/v1/users/me');
    expect(me.request.headers.get('X-Client-Action-Id')).toBe(actionId);
    me.flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(completed).toBe(true);
    expect(service.currentUser()).toBeNull();
    expect(correlation.getClientActionId()).toBeNull();
  });

  it('loads the current user profile when a session exists', () => {
    service.initialize().subscribe();
    requests.expectOne('/api/v1/csrf').flush(null);
    requests.expectOne('/api/v1/users/me').flush({ id: 'user', userName: 'Tenant', email: 'user@example.org', profile: null });
    expect(service.currentUser()?.email).toBe('user@example.org');
  });

  it('refreshes CSRF after login with the same action identifier', () => {
    let state = '';
    service.login('user@example.org', 'password').subscribe(result => state = result.state);
    const login = requests.expectOne('/api/v1/auth/login');
    const actionId = login.request.headers.get('X-Client-Action-Id');
    login.flush({ state: 'AUTHENTICATED', userId: 'user' });
    const csrf = requests.expectOne('/api/v1/csrf');
    expect(csrf.request.headers.get('X-Client-Action-Id')).toBe(actionId);
    csrf.flush(null);
    const me = requests.expectOne('/api/v1/users/me');
    expect(me.request.headers.get('X-Client-Action-Id')).toBe(actionId);
    me.flush({ id: 'user', userName: 'Tenant', email: 'user@example.org', profile: null });
    expect(state).toBe('AUTHENTICATED');
    expect(service.currentUser()?.id).toBe('user');
    expect(correlation.getClientActionId()).toBeNull();
  });

  it('keeps the pending login anonymous until a role is selected', () => {
    service.login('user@example.org', 'password').subscribe();
    requests.expectOne('/api/v1/auth/login').flush({ state: 'ROLE_SELECTION_REQUIRED', eligibleRoles: ['Tenant'] });
    requests.expectNone('/api/v1/csrf');
    expect(service.currentUser()).toBeNull();

    service.selectRole('Tenant').subscribe();
    requests.expectOne('/api/v1/auth/select-role').flush({ state: 'AUTHENTICATED', activeRole: 'Tenant' });
    requests.expectOne('/api/v1/csrf').flush(null);
    requests.expectOne('/api/v1/users/me').flush({ id: 'user', userName: 'Tenant', email: 'user@example.org', profile: null });
    expect(service.currentUser()?.userName).toBe('Tenant');
  });

  it('refreshes CSRF and reloads the user after switching role', () => {
    service.switchRole('Landlord').subscribe();
    requests.expectOne('/api/v1/auth/switch-role').flush(null);
    requests.expectOne('/api/v1/csrf').flush(null);
    requests.expectOne('/api/v1/users/me').flush({ id: 'user', userName: 'Landlord', email: 'user@example.org', profile: null });
    expect(service.currentUser()?.userName).toBe('Landlord');
  });

  it('waits for an in-flight CSRF refresh before another unsafe request', () => {
    service.login('user@example.org', 'password').subscribe();
    requests.expectOne('/api/v1/auth/login').flush({ state: 'AUTHENTICATED' });
    const refresh = requests.expectOne('/api/v1/csrf');
    service.logout().subscribe();
    requests.expectNone('/api/v1/auth/logout');

    refresh.flush(null);

    requests.expectOne('/api/v1/users/me').flush({ id: 'user', userName: 'Tenant', email: 'user@example.org', profile: null });
    requests.expectOne('/api/v1/auth/logout').flush(null);
    requests.expectOne('/api/v1/csrf').flush(null);
  });

  it('does not replay login when CSRF validation fails', () => {
    let failed = false;
    service.login('user@example.org', 'password').subscribe({ error: () => failed = true });
    requests.expectOne('/api/v1/auth/login').flush({}, { status: 400, statusText: 'Bad Request' });
    expect(failed).toBe(true);
    requests.expectNone('/api/v1/auth/login');
    requests.expectNone('/api/v1/csrf');
    expect(correlation.getClientActionId()).toBeNull();
  });
});
