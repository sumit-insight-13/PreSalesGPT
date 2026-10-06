import axios from "axios";

export const API_BASE_URL = "http://localhost:5217/api";

export const http = axios.create({
  baseURL: API_BASE_URL,
  timeout: 300000,
});

export function errorMessage(
  error: unknown,
  fallback: string
): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as
      | { message?: string }
      | undefined;

    if (data?.message) {
      return data.message;
    }
  }

  return fallback;
}
