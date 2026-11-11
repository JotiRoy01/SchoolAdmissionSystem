# Assumptions

- Each shift is stored as a separate class record, for example `Class 6
  Morning Shift` and `Class 6 Day Shift`.
- No authentication is required by the assignment. The 17-digit birth
  registration number identifies a student and is sent in `X-Student-Id`.
- A student cannot have more than one `HELD` or `CONFIRMED` application for
  the same class. Applications for different classes are allowed.
- Clicking the admission payment button represents a successful payment; no
  payment gateway or fee amount is required. Payment is persisted separately
  from confirmation, and confirmation is only allowed after payment succeeds.
- `HOLD_DURATION_SECONDS` defaults to 600 seconds. The expiry timestamp is
  persisted on each hold and is also enforced lazily by API operations.
- PostgreSQL row locking on the class record serializes seat allocation.
  Therefore simultaneous requests, including requests from multiple backend
  processes using the same database, cannot exceed the class capacity.
- The admin screen is intentionally unauthenticated because the assignment
  explicitly says login is not required.
- Admin class deletion removes the class and its associated applications in a
  single transaction. A deleted class is treated as a removed admission
  offering, so its applications are no longer part of the student's history.
