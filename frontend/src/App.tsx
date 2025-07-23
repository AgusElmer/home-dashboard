// This is the main component of your application. It manages the state and renders the UI.

import { useEffect, useState } from 'react';
import { api } from './api'; // Import the API helper function.

// Define the structure of a Note object, which should match the backend's Note model.
interface Note {
  id: number;
  text: string;
  createdAt: string;
}

// The main App component.
export default function App() {
  // `useState` is a React Hook that lets you add state to your components.
  // `email` stores the logged-in user's email.
  const [email, setEmail] = useState('');
  // `notes` stores the list of notes fetched from the backend.
  const [notes, setNotes] = useState<Note[]>([]);
  // `text` stores the value of the new note input field.
  const [text, setText] = useState('');
  // `loggedIn` tracks the user's authentication status. It's initialized to `null` to represent a loading state.
  const [loggedIn, setLoggedIn] = useState<boolean | null>(null);

  // A helper function to read a specific cookie by name.
  function getCookie(name: string): string | null {
    const v = "; " + document.cookie;
    const parts = v.split("; " + name + "=");
    if (parts.length === 2) return parts.pop()!.split(";")[0];
    return null;
  }

  // An asynchronous function to handle the user logout process.
  async function handleLogout() {
    try {
      // Get the CSRF token from the cookie.
      const csrf = getCookie("XSRF-TOKEN") ?? "";
      // Create the headers for the logout request, including the CSRF token.
      const headers: Record<string, string> = {
        "X-XSRF-TOKEN": csrf,
      };

      // Send a POST request to the backend's logout endpoint.
      await fetch("/auth/logout", {
        method: "POST",
        credentials: "include", // Send cookies with the request.
        headers,
      });

      // After a successful logout, update the state to reflect that the user is logged out.
      localStorage.removeItem("token"); // This line is not strictly necessary with cookie-based auth, but is good practice.
      setLoggedIn(false);
      setEmail('');
      setNotes([]);
    } catch (err) {
      console.error("Logout failed", err);
    }
  }

  // `useEffect` is a React Hook that lets you perform side effects in your components.
  // This effect runs once when the component mounts (due to the empty dependency array `[]`).
  useEffect(() => {
    // Try to fetch the current user's information.
    api<{ email: string }>('/me')
      .then(r => {
        // If successful, the user is logged in. Update the state with their email and load their notes.
        setEmail(r.email);
        setLoggedIn(true);
        loadNotes();
      })
      .catch(() => {
        // If the request fails, the user is not logged in.
        setLoggedIn(false);
      });
  }, []);

  // A function to fetch the user's notes from the backend.
  function loadNotes() {
    api<Note[]>('/notes').then(setNotes).catch(console.error);
  }

  // An asynchronous function to handle the submission of the new note form.
  async function addNote(e: React.FormEvent) {
    e.preventDefault(); // Prevent the default form submission behavior.
    if (!text.trim()) return; // Don't add empty notes.
    // Send a POST request to the backend to create a new note.
    await api<Note>('/notes', { method: 'POST', body: JSON.stringify({ text }) });
    // Clear the input field and reload the notes to show the new one.
    setText('');
    loadNotes();
  }

  // If the login status is still being determined, show a loading message.
  if (loggedIn === null) {
    return <div style={{ padding: 20 }}>Loading…</div>;
  }

  // If the user is not logged in, show the login page.
  if (!loggedIn) {
    return (
      <div style={{ padding: 20 }}>
        <h1>Home Dashboard</h1>
        {/* This link redirects the user to the backend's Google login endpoint. */}
        <a href="/auth/login-google">Login with Google</a>
      </div>
    );
  }

  // If the user is logged in, show the main dashboard.
  return (
    <div className="container">
      <h1>Dashboard</h1>
      <p>Logged in as {email}</p>

      {/* The form for adding a new note. */}
      <form onSubmit={addNote} style={{ marginBottom: 16 }}>
        <input
          value={text}
          onChange={e => setText(e.target.value)}
          placeholder="New note"
          style={{ width: '100%', padding: 6 }}
        />
        <button style={{ marginTop: 8 }}>Add</button>
      </form>

      {/* The list of notes. */}
      <ul className="note-list">
        {notes.map(n => (
          <li key={n.id}>
            {n.text} <small>({new Date(n.createdAt).toLocaleString()})</small>
          </li>
        ))}
      </ul>

      {/* The logout button. */}
      <button onClick={handleLogout}>
        Logout
      </button>
    </div>
  );
}