import { createApiClient } from '@netcha/shared';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5080';
export const CHALLENGE_URL = `${API_URL.replace(/\/$/, '')}${import.meta.env.VITE_CHALLENGE_PATH || '/api/v1/captcha/challenge'}`;
const VERIFY_PATH = import.meta.env.VITE_VERIFY_PATH || '/api/v1/captcha/verify';
const api = createApiClient(API_URL);

export interface DemoRequest {
  payload: string;
}

export interface VerificationResponse {
  verified: boolean;
  error: string | null;
}

export function verifyCaptcha(payload: DemoRequest) {
  return api.request<VerificationResponse>(VERIFY_PATH, {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}
