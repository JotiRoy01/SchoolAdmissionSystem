
import { useCallback, useEffect, useState } from "react";
import axios from "axios";

import { getMyHolds } from "../api/api";
import type { AdmissionHold } from "../types/admission";
import { useStudentIdentity } from "../context/StudentIdentityContext";

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;

    if (typeof data?.message === "string") return data.message;
    if (typeof data?.Message === "string") return data.Message;

    if (Array.isArray(data?.errors) && data.errors.length > 0) {
      return data.errors.join(" ");
    }

    if (Array.isArray(data?.Errors) && data.Errors.length > 0) {
      return data.Errors.join(" ");
    }

    return error.message || "Could not retrieve applications.";
  }

  return "An unexpected error occurred.";
}

function MyApplicationsPage() {
  const { studentId, setStudentId } = useStudentIdentity();

  const [applications, setApplications] = useState<AdmissionHold[]>([]);
  const [searched, setSearched] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const loadApplications = useCallback(async (showLoading = true) => {
    setError("");

    if (!/^[0-9]{17}$/.test(studentId)) {
      setError("Student ID must contain exactly 17 digits.");
      return;
    }

    try {
      if (showLoading) setLoading(true);

      const result = await getMyHolds(studentId);

      setApplications(result);
      setSearched(true);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      if (showLoading) setLoading(false);
    }
  }, [studentId]);

  useEffect(() => {
    if (!/^[0-9]{17}$/.test(studentId)) return;

    void loadApplications();
    const timer = window.setInterval(() => void loadApplications(false), 5000);

    return () => window.clearInterval(timer);
  }, [studentId, loadApplications]);

  const handleSearch = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await loadApplications();
  };

  return (
    <main className="page-container">
      <h1>My Applications</h1>
      <p>Enter your student ID to retrieve your admission applications.</p>

      <section className="application-panel">
        <form onSubmit={handleSearch}>
          <label htmlFor="studentId">
            Student Birth Registration Number
          </label>

          <input
            id="studentId"
            type="text"
            inputMode="numeric"
            maxLength={17}
            value={studentId}
            onChange={(event) =>
              setStudentId(event.target.value)
            }
            placeholder="Enter your 17-digit student ID"
            required
          />

          <button
            type="submit"
            className="primary-button"
            disabled={loading}
          >
            {loading ? "Searching..." : "Find my applications"}
          </button>
        </form>

        {error && <p className="error-message">{error}</p>}
      </section>

      {searched && (
        <section className="applications-list">
          <h2>Application history</h2>

          {applications.length === 0 ? (
            <p>No applications were found for this student ID.</p>
          ) : (
            applications.map((application) => (
              <article className="application-card" key={application.id}>
                <h3>{application.className}</h3>

                <p>
                  Application ID: <strong>{application.id}</strong>
                </p>

                <p>
                  Status:{" "}
                  <strong className={`status-${application.status.toLowerCase()}`}>
                    {application.status}
                  </strong>
                </p>

                <p>
                  Applied:{" "}
                  {new Date(application.createdAt).toLocaleString()}
                </p>

                {application.status === "HELD" && (
                  <p>
                    Hold expires:{" "}
                    {new Date(application.expiresAt).toLocaleString()}
                  </p>
                )}

                {application.confirmedAt && (
                  <p>
                    Confirmed:{" "}
                    {new Date(application.confirmedAt).toLocaleString()}
                  </p>
                )}
              </article>
            ))
          )}
        </section>
      )}
    </main>
  );
}

export default MyApplicationsPage;
