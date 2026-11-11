
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import axios from "axios";

import {
  getClassById,
  createHold,
  getHoldById,
  confirmHold,
  cancelHold,
  getMyHolds
} from "../api/api";

import type {
  AdmissionClass,
  AdmissionHold
} from "../types/admission";

import { useStudentIdentity } from "../context/StudentIdentityContext";

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;

    if (typeof data?.message === "string") {
      return data.message;
    }

    if (typeof data?.Message === "string") {
      return data.Message;
    }

    if (Array.isArray(data?.errors) && data.errors.length > 0) {
      return data.errors.join(" ");
    }

    if (Array.isArray(data?.Errors) && data.Errors.length > 0) {
      return data.Errors.join(" ");
    }

    if (error.response?.status === 409) {
      return "This student already has an active application for this class, or no seats are available.";
    }

    if (error.response?.status === 403) {
      return "You are not authorized to perform this action for this student.";
    }

    if (error.response?.status === 404) {
      return "The requested class or application could not be found.";
    }

    return error.message || "The request could not be completed.";
  }

  return "An unexpected error occurred.";
}

function formatRemainingTime(seconds: number): string {
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;

  return `${minutes}:${String(remainingSeconds).padStart(2, "0")}`;
}

function ApplicationPage() {
  const { classId } = useParams<{ classId: string }>();

  const { studentId, setStudentId } = useStudentIdentity();

  const [admissionClass, setAdmissionClass] =
    useState<AdmissionClass | null>(null);

  // Local input prevents the input from losing focus while typing.
  const [brnInput, setBrnInput] = useState(studentId);

  const [hold, setHold] = useState<AdmissionHold | null>(null);
  const [remainingSeconds, setRemainingSeconds] = useState(0);

  const [loading, setLoading] = useState(true);
  const [restoringApplication, setRestoringApplication] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  // Synchronize the input when the navbar student identity changes.
  useEffect(() => {
    setBrnInput(studentId);
  }, [studentId]);

  // Load the selected admission class.
  useEffect(() => {
    let cancelled = false;

    const loadClass = async () => {
      if (!classId) {
        setError("Invalid admission class.");
        setAdmissionClass(null);
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError("");

        const result = await getClassById(classId);

        if (!cancelled) {
          setAdmissionClass(result);
        }
      } catch (err) {
        if (!cancelled) {
          setAdmissionClass(null);
          setError(getErrorMessage(err));
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    loadClass();

    return () => {
      cancelled = true;
    };
  }, [classId]);

  // Find an existing application for the currently entered student.
  useEffect(() => {
    let cancelled = false;

    // Clear the previous student's application immediately.
    setHold(null);
    setError("");

    if (!classId || !/^[0-9]{17}$/.test(brnInput)) {
      setRestoringApplication(false);

      return () => {
        cancelled = true;
      };
    }

    setRestoringApplication(true);

    // Debounce the API request while the user is typing.
    const timer = window.setTimeout(async () => {
      try {
        const applications = await getMyHolds(brnInput);

        const existingApplication = applications.find(
          (application) =>
            application.classId === classId &&
            (application.status === "HELD" ||
              application.status === "CONFIRMED")
        );

        if (!cancelled) {
          setHold(existingApplication ?? null);
        }
      } catch (err) {
        if (!cancelled) {
          setHold(null);
          setError(getErrorMessage(err));
        }
      } finally {
        if (!cancelled) {
          setRestoringApplication(false);
        }
      }
    }, 400);

    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [classId, brnInput]);

  // Calculate remaining reservation time.
  useEffect(() => {
    if (!hold || hold.status !== "HELD") {
      setRemainingSeconds(0);
      return;
    }

    const updateCountdown = () => {
      const remaining = Math.max(
        0,
        Math.ceil(
          (new Date(hold.expiresAt).getTime() - Date.now()) / 1000
        )
      );

      setRemainingSeconds(remaining);
    };

    updateCountdown();

    const timer = window.setInterval(updateCountdown, 1000);

    return () => window.clearInterval(timer);
  }, [hold]);

  // Refresh the hold status from the backend every five seconds.
  useEffect(() => {
    if (!hold || hold.status !== "HELD") {
      return;
    }

    let cancelled = false;

    const refreshHold = async () => {
      try {
        const latest = await getHoldById(hold.id);

        if (!cancelled) {
          setHold(latest);
        }
      } catch (err) {
        console.error("Could not refresh hold status:", err);
      }
    };

    const timer = window.setInterval(refreshHold, 5000);

    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, [hold?.id, hold?.status]);

  // Handle BRN input changes.
  const handleBrnChange = (value: string) => {
    const numericValue = value.replace(/\D/g, "").slice(0, 17);

    // Remove the previous student's application from the screen.
    setHold(null);
    setError("");

    // Update local input only. Do not update navbar on every digit.
    setBrnInput(numericValue);
  };

  // Create a new admission reservation.
  const handleCreateHold = async (
    event: React.FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();
    setError("");

    if (!/^[0-9]{17}$/.test(brnInput)) {
      setError(
        "Student Birth Registration Number must contain exactly 17 digits."
      );
      return;
    }

    if (!classId) {
      setError("Invalid admission class.");
      return;
    }

    try {
      setSubmitting(true);

      // Update the shared student identity after submission.
      setStudentId(brnInput);

      const result = await createHold(classId, brnInput);

      setHold(result);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  // Simulate payment and confirm admission.
  const handleConfirm = async () => {
    if (!hold) return;

    try {
      setSubmitting(true);
      setError("");

      const result = await confirmHold(hold.id, hold.studentId);

      setHold(result);
    } catch (err) {
      setError(getErrorMessage(err));

      try {
        const latest = await getHoldById(hold.id);
        setHold(latest);
      } catch {
        // Keep the original error visible.
      }
    } finally {
      setSubmitting(false);
    }
  };

  // Cancel the current reservation.
  const handleCancel = async () => {
    if (!hold) return;

    try {
      setSubmitting(true);
      setError("");

      await cancelHold(hold.id, hold.studentId);

      setHold({
        ...hold,
        status: "CANCELLED"
      });
    } catch (err) {
      setError(getErrorMessage(err));

      try {
        const latest = await getHoldById(hold.id);
        setHold(latest);
      } catch {
        // Keep the original error visible.
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <main className="page-container">
        Loading class information...
      </main>
    );
  }

  if (!admissionClass) {
    return (
      <main className="page-container">
        <h1>Admission Application</h1>

        <p className="error-message">
          {error || "Class not found."}
        </p>

        <Link to="/">Back to classes</Link>
      </main>
    );
  }

  return (
    <main className="page-container">
      <h1>Admission Application</h1>

      <section className="application-panel">
        <h2>{admissionClass.name}</h2>

        <p>
          Available seats:{" "}
          <strong>{admissionClass.availableSeats}</strong>
        </p>

        {/* Keep the BRN input visible even when an application exists. */}
        <form onSubmit={handleCreateHold}>
          <label htmlFor="studentId">
            Student Birth Registration Number
          </label>

          <input
            id="studentId"
            type="text"
            inputMode="numeric"
            maxLength={17}
            value={brnInput}
            onChange={(event) =>
              handleBrnChange(event.target.value)
            }
            placeholder="Enter the student's 17-digit BRN"
            required
          />

          {/* Only show Reserve when this student has no active application. */}
          {!hold && (
            <button
              type="submit"
              className="primary-button"
              disabled={
                submitting ||
                restoringApplication ||
                !/^[0-9]{17}$/.test(brnInput) ||
                admissionClass.availableSeats <= 0
              }
            >
              {submitting
                ? "Reserving..."
                : restoringApplication
                  ? "Checking application..."
                  : "Reserve a seat"}
            </button>
          )}
        </form>

        {restoringApplication && (
          <p>Checking this student's existing application...</p>
        )}

        {error && (
          <p className="error-message">{error}</p>
        )}

        {/* Display the application belonging to the entered BRN. */}
        {hold && (
          <section className="hold-details">
            <h3>Application status</h3>

            <p>
              Application ID: <strong>{hold.id}</strong>
            </p>

            <p>
              Student ID: <strong>{hold.studentId}</strong>
            </p>

            <p>
              Status: <strong>{hold.status}</strong>
            </p>

            {hold.status === "HELD" && (
              <p>
                Payment: <strong>{hold.paymentStatus}</strong>
              </p>
            )}

            {hold.status === "HELD" && (
              <>
                <p>
                  Time remaining:{" "}
                  <strong>
                    {formatRemainingTime(remainingSeconds)}
                  </strong>
                </p>

                {remainingSeconds > 0 ? (
                  <>
                    <button
                      className="primary-button"
                      onClick={handleConfirm}
                      disabled={submitting || remainingSeconds <= 0}
                    >
                      {submitting
                        ? "Processing payment..."
                        : "Pay admission fee"}
                    </button>

                    <button
                      className="secondary-button"
                      onClick={handleCancel}
                      disabled={submitting}
                    >
                      Cancel reservation
                    </button>
                  </>
                ) : (
                  <p className="error-message">
                    Your hold has reached its expiry time.
                    Checking the latest status with the server...
                  </p>
                )}
              </>
            )}

            {hold.status === "CONFIRMED" && (
              <p className="success-message">
                Your admission reservation has been confirmed.
              </p>
            )}

            {hold.status === "EXPIRED" && (
              <p className="error-message">
                This reservation has expired. You can return
                to the class list to check availability.
              </p>
            )}

            {hold.status === "CANCELLED" && (
              <p>This reservation has been cancelled.</p>
            )}
          </section>
        )}

        <p>
          <Link to="/">Back to admission classes</Link>
        </p>
      </section>
    </main>
  );
}

export default ApplicationPage;
