CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE admission_classes (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    total_seats integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_admission_classes" PRIMARY KEY (id),
    CONSTRAINT ck_admission_classes_total_seats CHECK ("total_seats" > 0)
);

CREATE TABLE students (
    id uuid NOT NULL,
    birth_registration_number character varying(17) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_students" PRIMARY KEY (id),
    CONSTRAINT ck_students_birth_registration_number CHECK ("birth_registration_number" ~ '^[0-9]{17}$')
);

CREATE TABLE admission_holds (
    id uuid NOT NULL,
    class_id uuid NOT NULL,
    student_id uuid NOT NULL,
    status character varying(20) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    confirmed_at timestamp with time zone,
    CONSTRAINT "PK_admission_holds" PRIMARY KEY (id),
    CONSTRAINT ck_admission_holds_status CHECK ("status" IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')),
    CONSTRAINT "FK_admission_holds_admission_classes_class_id" FOREIGN KEY (class_id) REFERENCES admission_classes (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_admission_holds_students_student_id" FOREIGN KEY (student_id) REFERENCES students (id) ON DELETE RESTRICT
);

CREATE INDEX ix_admission_holds_class_status ON admission_holds (class_id, status);

CREATE INDEX ix_admission_holds_expires_at ON admission_holds (expires_at) WHERE "status" = 'HELD';

CREATE UNIQUE INDEX ux_active_student_class ON admission_holds (student_id, class_id) WHERE "status" IN ('HELD', 'CONFIRMED');

CREATE UNIQUE INDEX ux_students_birth_registration_number ON students (birth_registration_number);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261109081912_InitialCreate', '10.0.14');

COMMIT;

