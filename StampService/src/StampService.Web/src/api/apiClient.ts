import { authStorage } from '../auth/authStorage';

export type ApiError = {
  code: string;
  message: string;
  type: string;
  invalidField?: string | null;
  metadata?: Record<string, unknown> | null;
};

type ApiEnvelope<T> = {
  result?: T | null;
  errors?: ApiError[] | null;
  isError?: boolean;
  timeGenerated?: string;
};

export class ApiRequestError extends Error {
  readonly status: number;
  readonly errors: ApiError[];

  constructor(status: number, errors: ApiError[], fallbackMessage: string) {
    super(errors[0]?.message || fallbackMessage);
    this.name = 'ApiRequestError';
    this.status = status;
    this.errors = errors;
  }
}

export const unauthorizedEventName = 'stampservice:unauthorized';

type ApiRequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  authenticated?: boolean;
};

export async function apiRequest<T>(
  path: string,
  options: ApiRequestOptions = {}
): Promise<T> {
  const headers = new Headers();

  if (options.body !== undefined) {
    headers.set('Content-Type', 'application/json');
  }

  if (options.authenticated ?? true) {
    const token = authStorage.getToken();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
  }

  let response: Response;
  try {
    response = await fetch(path, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined
    });
  } catch {
    throw new ApiRequestError(
      0,
      [],
      'Нет связи с backend API. Проверьте, что StampService.API запущен.'
    );
  }

  const envelope = await readEnvelope<T>(response);

  if (!response.ok || envelope.errors?.length) {
    if (response.status === 401) {
      window.dispatchEvent(new Event(unauthorizedEventName));
    }

    throw new ApiRequestError(
      response.status,
      envelope.errors ?? [],
      response.status === 401 ? 'Сессия истекла. Войдите снова.' : 'Не удалось выполнить запрос.'
    );
  }

  return envelope.result as T;
}

async function readEnvelope<T>(response: Response): Promise<ApiEnvelope<T>> {
  const text = await response.text();
  if (!text) {
    return {};
  }

  try {
    return JSON.parse(text) as ApiEnvelope<T>;
  } catch {
    return {
      errors: [
        {
          code: 'client.invalid_response',
          message: 'Сервер вернул некорректный ответ.',
          type: 'Failure'
        }
      ]
    };
  }
}
