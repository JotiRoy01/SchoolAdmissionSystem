import { useEffect, useState } from "react";
import axios from "axios";

import { createClass, deleteClass, getAllClasses } from "../api/api";
import type { AdmissionClass } from "../types/admission";

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const message = error.response?.data?.message ?? error.response?.data?.Message;
    return typeof message === "string"
      ? message
      : "The class could not be created.";
  }

  return "The class could not be created.";
}

function AdminPage() {
  const [name, setName] = useState("");
  const [totalSeats, setTotalSeats] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [classes, setClasses] = useState<AdmissionClass[]>([]);

  const loadClasses = async () => {
    try {
      setClasses(await getAllClasses());
    } catch (requestError) {
      setError(getErrorMessage(requestError));
    }
  };

  useEffect(() => {
    void loadClasses();
  }, []);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setMessage("");
    setError("");

    const seats = Number(totalSeats);
    if (!name.trim() || !Number.isInteger(seats) || seats < 1) {
      setError("Enter a class name and a positive whole number of seats.");
      return;
    }

    try {
      setSubmitting(true);
      const created = await createClass(name.trim(), seats);
      setMessage(`${created.name} was created successfully.`);
      setName("");
      setTotalSeats("");
      await loadClasses();
    } catch (requestError) {
      setError(getErrorMessage(requestError));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (admissionClass: AdmissionClass) => {
    if (!window.confirm(`Delete ${admissionClass.name}?`)) return;

    setError("");
    try {
      await deleteClass(admissionClass.id);
      setMessage(`${admissionClass.name} was deleted.`);
      await loadClasses();
    } catch (requestError) {
      setError(getErrorMessage(requestError));
    }
  };

  return (
    <main className="page-container">
      <h1>School administration</h1>
      <p>Create each shift as a separate admission class.</p>

      <section className="application-panel">
        <form onSubmit={handleSubmit}>
          <label htmlFor="className">Class or shift name</label>
          <input
            id="className"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Class 6 Morning Shift"
            maxLength={100}
            required
          />

          <label htmlFor="totalSeats">Total seats</label>
          <input
            id="totalSeats"
            type="number"
            min="1"
            value={totalSeats}
            onChange={(event) => setTotalSeats(event.target.value)}
            required
          />

          <button className="primary-button" disabled={submitting}>
            {submitting ? "Creating..." : "Create class"}
          </button>
        </form>

        {message && <p className="success-message">{message}</p>}
        {error && <p className="error-message">{error}</p>}
      </section>

      <section className="applications-list">
        <h2>Admission classes</h2>
        {classes.map((admissionClass) => (
          <article className="application-card" key={admissionClass.id}>
            <h3>{admissionClass.name}</h3>
            <p>
              {admissionClass.availableSeats} of {admissionClass.totalSeats} seats available
            </p>
            <button
              className="secondary-button"
              onClick={() => void handleDelete(admissionClass)}
            >
              Delete class
            </button>
          </article>
        ))}
      </section>
    </main>
  );
}

export default AdminPage;
