
import { BrowserRouter, Routes, Route } from "react-router-dom";

import Navbar from "./components/Navbar";
import HomePage from "./pages/HomePage";
import ApplicationPage from "./pages/ApplicationPage";
import MyApplicationsPage from "./pages/MyApplicationsPage";
import AdminPage from "./pages/AdminPage";
// import PaymentPage from "./pages/PaymentPage";
import { StudentIdentityProvider } from "./context/StudentIdentityContext";

function App() {
  return (
    <StudentIdentityProvider>
      <BrowserRouter>
        <Navbar />

        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/apply/:classId" element={<ApplicationPage />} />
          <Route path="/my-applications" element={<MyApplicationsPage />} />
          <Route path="/admin" element={<AdminPage />} />
        </Routes>
      </BrowserRouter>
    </StudentIdentityProvider>
  );
}

export default App;
