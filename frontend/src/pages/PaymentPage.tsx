// import { useEffect, useState } from "react";
// import { Link, useNavigate, useParams } from "react-router-dom";
// import axios from "axios";

// import { getHoldById, payAdmissionFee } from "../api/api";
// import type { AdmissionHold } from "../types/admission";
// import { useStudentIdentity } from "../context/StudentIdentityContext";

// function getErrorMessage(error: unknown): string {
//   if (axios.isAxiosError(error)) {
//     const message = error.response?.data?.message ?? error.response?.data?.Message;
//     return typeof message === "string"
//       ? message
//       : "The payment could not be completed.";
//   }

//   return "The payment could not be completed.";
// }

// function PaymentPage() {
//   const { holdId } = useParams<{ holdId: string }>();
//   const navigate = useNavigate();
//   const { studentId } = useStudentIdentity();
//   const [hold, setHold] = useState<AdmissionHold | null>(null);
//   const [loading, setLoading] = useState(true);
//   const [submitting, setSubmitting] = useState(false);
//   const [error, setError] = useState("");

//   useEffect(() => {
//     if (!holdId) {
//       setError("Invalid application.");
//       setLoading(false);
//       return;
//     }

//     getHoldById(holdId)
//       .then(setHold)
//       .catch((requestError) => setError(getErrorMessage(requestError)))
//       .finally(() => setLoading(false));
//   }, [holdId]);

//   const handlePayment = async () => {
//     if (!holdId) return;

//     if (!/^[0-9]{17}$/.test(studentId)) {
//       setError("Enter your 17-digit student ID before paying.");
//       return;
//     }

//     try {
//       setSubmitting(true);
//       setError("");
//       setHold(await payAdmissionFee(holdId, studentId));
//     } catch (requestError) {
//       setError(getErrorMessage(requestError));
//     } finally {
//       setSubmitting(false);
//     }
//   };

//   if (loading) {
//     return <main className="page-container">Loading payment details...</main>;
//   }

//   if (!hold) {
//     return (
//       <main className="page-container">
//         <h1>Admission payment</h1>
//         <p className="error-message">{error || "Application not found."}</p>
//         <Link to="/">Back to classes</Link>
//       </main>
//     );
//   }

//   const paid = hold.paymentStatus === "PAID";
//   const expired = hold.status !== "HELD";

//   return (
//     <main className="page-container">
//       <h1>Admission payment</h1>
//       <section className="application-panel">
//         <h2>{hold.className}</h2>
//         <p>Application status: <strong>{hold.status}</strong></p>
//         <p>Payment status: <strong>{hold.paymentStatus}</strong></p>

//         {error && <p className="error-message">{error}</p>}

//         {paid ? (
//           <>
//             <p className="success-message">Payment completed successfully.</p>
//             <button
//               className="primary-button"
//               onClick={() => navigate(`/apply/${hold.classId}`)}
//             >
//               Continue to confirmation
//             </button>
//           </>
//         ) : expired ? (
//           <p className="error-message">
//             This application is no longer active, so payment is unavailable.
//           </p>
//         ) : (
//           <button
//             className="primary-button"
//             onClick={() => void handlePayment()}
//             disabled={submitting}
//           >
//             {submitting ? "Processing payment..." : "Pay admission fee"}
//           </button>
//         )}

//         <p><Link to={`/apply/${hold.classId}`}>Back to application</Link></p>
//       </section>
//     </main>
//   );
// }

// export default PaymentPage;