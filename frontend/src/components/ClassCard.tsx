
import { useNavigate } from "react-router-dom";
import type { AdmissionClass } from "../types/admission";

interface ClassCardProps {
  admissionClass: AdmissionClass;
}

function ClassCard({ admissionClass }: ClassCardProps) {
  const navigate = useNavigate();

  const isFull = admissionClass.availableSeats <= 0;

  return (
    <article className="class-card">
      <div className="class-card-header">
        <div>
          <span className="class-label">ADMISSION OPEN</span>
          <h2>{admissionClass.name}</h2>
        </div>

        <span className={`seat-status ${isFull ? "full" : "available"}`}>
          {isFull ? "Full" : "Seats available"}
        </span>
      </div>

      <div className="seat-summary">
        <div className="seat-item">
          <strong>{admissionClass.totalSeats}</strong>
          <span>Total seats</span>
        </div>

        <div className="seat-item">
          <strong>{admissionClass.confirmedSeats}</strong>
          <span>Confirmed</span>
        </div>

        <div className="seat-item">
          <strong>{admissionClass.heldSeats}</strong>
          <span>On hold</span>
        </div>

        <div className="seat-item">
          <strong>{admissionClass.availableSeats}</strong>
          <span>Available</span>
        </div>
      </div>

      <button
        className="primary-button"
        disabled={isFull}
        onClick={() =>
          navigate(`/apply/${admissionClass.id}`)
        }
      >
        {isFull ? "No seats available" : "Apply for admission"}
      </button>
    </article>
  );
}

export default ClassCard;
