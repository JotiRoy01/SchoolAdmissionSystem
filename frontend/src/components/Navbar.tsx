
import { Link, NavLink } from "react-router-dom";
import { useStudentIdentity } from "../context/StudentIdentityContext";

function Navbar() {
  const { studentId, setStudentId } = useStudentIdentity();

  return (
    <header className="navbar">
      <Link to="/" className="brand">
        <span className="brand-icon">S</span>
        <span>School Admission</span>
      </Link>

      <nav>
        <NavLink to="/" end>
          Home
        </NavLink>

        <NavLink to="/my-applications">
          My Applications
        </NavLink>

        <NavLink to="/admin">
          Admin
        </NavLink>
      </nav>

      <label className="student-identity">
        <span>Student ID</span>
        <input
          value={studentId}
          inputMode="numeric"
          maxLength={17}
          placeholder="17-digit birth registration number"
          onChange={(event) => setStudentId(event.target.value)}
          aria-label="Student birth registration number"
        />
      </label>
    </header>
  );
}

export default Navbar;
