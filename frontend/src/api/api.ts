
import axios from "axios";
import type { AdmissionClass, AdmissionHold } from "../types/admission";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: {
    "Content-Type": "application/json"
  }
});

// Class Management

export const getAllClasses = async (): Promise<AdmissionClass[]> => {
  const response = await api.get<AdmissionClass[]>("/classes");
  return response.data;
};

export const createClass = async (
  name: string,
  totalSeats: number
): Promise<AdmissionClass> => {
  const response = await api.post<AdmissionClass>("/classes", {
    name,
    totalSeats
  });
  return response.data;
};

export const getClassById = async (
  classId: string
): Promise<AdmissionClass> => {
  const response = await api.get<AdmissionClass>(
    `/classes/${classId}`
  );
  return response.data;
};

export const deleteClass = async (classId: string): Promise<void> => {
  await api.delete(`/classes/${classId}`);
};

// Hold Management

export const createHold = async (
  classId: string,
  studentId: string
): Promise<AdmissionHold> => {
  const response = await api.post<AdmissionHold>(
    `/classes/${classId}/holds`,
    null,
    {
      headers: {
        "X-Student-Id": studentId
      }
    }
  );

  return response.data;
};

export const getHoldById = async (
  holdId: string
): Promise<AdmissionHold> => {
  const response = await api.get<AdmissionHold>(
    `/holds/${holdId}`
  );

  return response.data;
};

export const confirmHold = async (
  holdId: string,
  studentId: string
): Promise<AdmissionHold> => {
  const response = await api.post<AdmissionHold>(
    `/holds/${holdId}/confirm`,
    null,
    {
      headers: {
        "X-Student-Id": studentId
      }
    }
  );

  return response.data;
};

export const payAdmissionFee = async (
  holdId: string,
  studentId: string
): Promise<AdmissionHold> => {
  const response = await api.post<AdmissionHold>(
    `/holds/${holdId}/payment`,
    null,
    {
      headers: {
        "X-Student-Id": studentId
      }
    }
  );

  return response.data;
};

// export const cancelHold = async (
//   holdId: string,
//   studentId: string
// ): Promise<AdmissionHold> => {
//   const response = await api.delete<AdmissionHold>(
//     `/holds/${holdId}`,
//     {
//       headers: {
//         "X-Student-Id": studentId
//       }
//     }
//   );

//   return response.data;
// };


export const cancelHold = async (
  holdId: string,
  studentId: string
): Promise<void> => {
  await api.delete(
    `/holds/${holdId}`,
    {
      headers: {
        "X-Student-Id": studentId
      }
    }
  );
};


export const getMyHolds = async (
  studentId: string
): Promise<AdmissionHold[]> => {
  const response = await api.get<AdmissionHold[]>(
    "/me/holds",
    {
      headers: {
        "X-Student-Id": studentId
      }
    }
  );

  return response.data;
};

export default api;
