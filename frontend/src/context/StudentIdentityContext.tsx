import { createContext, useContext, useState, type ReactNode } from "react";

const storageKey = "admissionStudentId";

interface StudentIdentityContextValue {
  studentId: string;
  setStudentId: (studentId: string) => void;
}

const StudentIdentityContext = createContext<StudentIdentityContextValue | null>(null);

export function StudentIdentityProvider({ children }: { children: ReactNode }) {
  const [studentId, setStudentIdState] = useState(
    () => localStorage.getItem(storageKey) ?? ""
  );

  const setStudentId = (value: string) => {
    const normalized = value.replace(/\D/g, "").slice(0, 17);
    setStudentIdState(normalized);

    if (normalized) {
      localStorage.setItem(storageKey, normalized);
    } else {
      localStorage.removeItem(storageKey);
    }
  };

  return (
    <StudentIdentityContext.Provider value={{ studentId, setStudentId }}>
      {children}
    </StudentIdentityContext.Provider>
  );
}

export function useStudentIdentity() {
  const context = useContext(StudentIdentityContext);

  if (!context) {
    throw new Error("useStudentIdentity must be used inside StudentIdentityProvider");
  }

  return context;
}