// This file configures Vite, the build tool used for the frontend.
// Vite is responsible for bundling your code for production and running the development server.

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The main configuration for Vite.
// For more information, visit https://vitejs.dev/config/
export default defineConfig({
  // Set the base URL for the application. This is crucial for correct routing and asset loading in production.
  base: process.env.VITE_FRONTEND_BASE_URL || '/',
  // Plugins extend Vite's functionality. `@vitejs/plugin-react` adds support for React.
  plugins: [react()],
  // Configuration for the development server.
  server: {
    // The proxy is a crucial piece for development. It forwards specific requests from the frontend to the backend server.
    // This avoids CORS issues that would otherwise occur when your frontend (e.g., at localhost:5173) tries to call your backend (e.g., at localhost:5059).
    proxy: {
      // Any request starting with `/api` will be forwarded to the backend.
      // For example, a call to `/api/notes` in your frontend code will be sent to `http://localhost:5059/api/notes`.
      '/api': 'http://localhost:8080',
      // Similarly, any request starting with `/auth` is forwarded to the backend.
      // This is used for the Google authentication flow.
      '/auth': 'http://localhost:8080'
    }
  }
});