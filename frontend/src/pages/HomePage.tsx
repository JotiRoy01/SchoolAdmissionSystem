
import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";

import { getAllClasses } from "../api/api";
import type { AdmissionClass } from "../types/admission";

import ClassCard from "../components/ClassCard";
import Loading from "../components/Loading";

function HomePage() {
  const [classes, setClasses] = useState<AdmissionClass[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const hasLoaded = useRef(false);

  useEffect(() => {
    let cancelled = false;

    const fetchClasses = async () => {
      try {
        if (!hasLoaded.current) setLoading(true);
        setError("");

        const data = await getAllClasses();
        if (!cancelled) {
          setClasses(data);
          hasLoaded.current = true;
        }
      } catch (err) {
        console.error("Failed to fetch classes:", err);
        if (!cancelled) setError("Unable to load admission classes. Please try again.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    fetchClasses();
    const timer = window.setInterval(fetchClasses, 4000);

    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, []);

  return (
    <main className="page-container">
      <section className="hero-section">
        <span className="hero-label">SCHOOL ADMISSION PORTAL</span>

        <h1>
          Start Your Child's
          <br />
          Educational Journey
        </h1>

        <p>
          Explore available classes and reserve an admission
          seat through our online admission system.
        </p>

        <Link to="/my-applications" className="secondary-button">
          Track My Applications
        </Link>
      </section>

      <section className="classes-section">
        <div className="section-heading">
          <div>
            <span className="section-label">EXPLORE</span>
            <h2>Available Admission Classes</h2>
          </div>

          <span>{classes.length} classes</span>
        </div>

        {loading && <Loading />}

        {!loading && error && (
          <div className="error-message">
            <p>{error}</p>
            <button
              className="secondary-button"
              onClick={() => window.location.reload()}
            >
              Try again
            </button>
          </div>
        )}

        {!loading && !error && classes.length === 0 && (
          <div className="empty-message">
            No admission classes are currently available.
          </div>
        )}

        {!loading && !error && classes.length > 0 && (
          <div className="class-grid">
            {classes.map((admissionClass) => (
              <ClassCard
                key={admissionClass.id}
                admissionClass={admissionClass}
              />
            ))}
          </div>
        )}
      </section>
    </main>
  );
}

export default HomePage;
