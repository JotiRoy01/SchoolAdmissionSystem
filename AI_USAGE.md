# AI usage

AI assistance was used to review the Bangla assignment, identify API and
concurrency requirements, and suggest implementation and validation steps.
The generated suggestions were checked against the source code, EF Core
configuration, API routes, and Docker configuration before being applied.

The important correctness checks were performed manually and with builds:
database row locking is used for seat allocation, filtered uniqueness prevents
duplicate active applications, expiry is enforced by both the worker and API
operations, and the Docker database uses a persistent volume.
