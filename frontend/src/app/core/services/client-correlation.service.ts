import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ClientCorrelationService {
  private activeActionId: string | null = null;

  startAction(): string {
    const actionId = crypto.randomUUID();
    this.activeActionId = actionId;
    return actionId;
  }

  getClientActionId(): string | null {
    return this.activeActionId;
  }

  completeAction(actionId?: string): void {
    if (!actionId || this.activeActionId === actionId) {
      this.activeActionId = null;
    }
  }

  createRequestId(): string {
    return crypto.randomUUID();
  }
}
