import { ApiRequestError } from './apiClient';

export function getApiErrorMessage(error: unknown, fallbackMessage: string): string {
  if (error instanceof ApiRequestError) {
    return error.message;
  }

  return fallbackMessage;
}
