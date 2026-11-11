
export interface AdmissionClass {
  id: string;
  name: string;
  totalSeats: number;
  confirmedSeats: number;
  heldSeats: number;
  availableSeats: number;
  createdAt: string;
}

export type HoldStatus =
  | "HELD"
  | "CONFIRMED"
  | "EXPIRED"
  | "CANCELLED";

export type PaymentStatus = "PENDING" | "PAID";

export interface AdmissionHold {
  id: string;
  classId: string;
  className: string;
  studentId: string;
  status: HoldStatus;
  createdAt: string;
  expiresAt: string;
  confirmedAt: string | null;
  paymentStatus: PaymentStatus;
  paidAt: string | null;
}
