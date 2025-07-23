// This file provides a helper function for making API calls to the backend.
// It centralizes the logic for handling things like headers, credentials, and CSRF tokens.

// A helper function to read the CSRF token from the `XSRF-TOKEN` cookie.
// This is necessary because the cookie is not `HttpOnly`, so it can be read by JavaScript.
function getCsrfTokenFromCookie(): string | null {
  // This regular expression finds the value of the `XSRF-TOKEN` cookie.
  const m = document.cookie.match(/(?:^|;\s*)XSRF-TOKEN=([^;]+)/);
  // If the cookie is found, its value is decoded and returned.
  return m ? decodeURIComponent(m[1]) : null;
}

// The main `api` function for making requests to the backend.
// It's a generic function, so you can specify the expected response type.
export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  // Create a new Headers object to manage request headers.
  const headers = new Headers(init.headers);
  // Set the `Content-Type` header to `application/json` for all requests.
  headers.set('Content-Type', 'application/json');

  // For any request that is not a GET request, we need to add the CSRF token to the headers.
  const method = (init.method ?? 'GET').toUpperCase();
  if (method !== 'GET') {
    const csrf = getCsrfTokenFromCookie();
    if (csrf) {
      // The backend will check for this header to prevent CSRF attacks.
      headers.set('X-XSRF-TOKEN', csrf);
    }
  }

  // Use the `fetch` API to make the request.
  // The path is prefixed with `/api` to match the proxy configuration in `vite.config.ts`.
  const res = await fetch('/api' + path, {
    ...init, // Spread any custom options for the request.
    headers, // Add the configured headers.
    credentials: 'include' // This is crucial! It tells the browser to send cookies with the request.
  });

  // If the response is not successful, throw an error.
  if (!res.ok) throw new Error(await res.text());
  // Otherwise, parse the JSON response and return it.
  return res.json() as Promise<T>;
}