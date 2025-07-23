// This file is the main entry point for your React application.
// It's responsible for rendering the root component (`App`) into the DOM.

import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App'; // The root component of your application.
import './index.css'; // Global styles for your application.

// `ReactDOM.createRoot` creates a new React root, which is the starting point for rendering your application.
// `document.getElementById('root')` selects the `<div>` with the ID `root` from `index.html`.
ReactDOM.createRoot(document.getElementById('root')!).render(
  // `React.StrictMode` is a tool for highlighting potential problems in an application.
  // It activates additional checks and warnings for its descendants.
  <React.StrictMode>
    {/* The `App` component is the root of your application's component tree. */}
    <App />
  </React.StrictMode>
);