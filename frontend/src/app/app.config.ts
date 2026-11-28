import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { clientCorrelationInterceptor } from './core/interceptors/client-correlation.interceptor';
import { AuthenticationService } from './core/services/authentication.service';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([clientCorrelationInterceptor])),
    provideAppInitializer(() => firstValueFrom(inject(AuthenticationService).initialize())),
    provideRouter(routes)
  ]
};
