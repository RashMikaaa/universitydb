-- ============================================
-- UNIVERSITY ACADEMIC PROGRESS DATABASE
-- ============================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'UniversityDB')
BEGIN
    ALTER DATABASE UniversityDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE UniversityDB;
END
GO

CREATE DATABASE UniversityDB;
GO

USE UniversityDB;
GO

-- ============================================
-- TABLE: Faculties
-- ============================================
CREATE TABLE Faculties (
    FacultyID   INT IDENTITY(1,1) PRIMARY KEY,
    FacultyName NVARCHAR(100) NOT NULL
);
GO

-- ============================================
-- TABLE: Specialties
-- ============================================
CREATE TABLE Specialties (
    SpecialtyID   INT IDENTITY(1,1) PRIMARY KEY,
    SpecialtyName NVARCHAR(100) NOT NULL,
    FacultyID     INT NOT NULL,
    CONSTRAINT FK_Specialties_Faculties FOREIGN KEY (FacultyID)
        REFERENCES Faculties(FacultyID)
        ON DELETE CASCADE
);
GO

-- ============================================
-- TABLE: Students
-- ============================================
CREATE TABLE Students (
    StudentID   INT IDENTITY(1,1) PRIMARY KEY,
    FirstName   NVARCHAR(50)  NOT NULL,
    LastName    NVARCHAR(50)  NOT NULL,
    Age         INT           NOT NULL,
    Gender      NVARCHAR(10)  NOT NULL CHECK (Gender IN ('Male', 'Female')),
    CourseYear  INT           NOT NULL CHECK (CourseYear BETWEEN 1 AND 5),
    SpecialtyID INT           NOT NULL,
    CONSTRAINT FK_Students_Specialties FOREIGN KEY (SpecialtyID)
        REFERENCES Specialties(SpecialtyID)
        ON DELETE CASCADE
);
GO

-- ============================================
-- TABLE: WorkTypes
-- ============================================
CREATE TABLE WorkTypes (
    WorkTypeID   INT IDENTITY(1,1) PRIMARY KEY,
    WorkTypeName NVARCHAR(50) NOT NULL
);
GO

-- ============================================
-- TABLE: Subjects
-- ============================================
CREATE TABLE Subjects (
    SubjectID   INT IDENTITY(1,1) PRIMARY KEY,
    SubjectName NVARCHAR(100) NOT NULL,
    SpecialtyID INT NOT NULL,
    CONSTRAINT FK_Subjects_Specialties FOREIGN KEY (SpecialtyID)
        REFERENCES Specialties(SpecialtyID)
        ON DELETE CASCADE
);
GO

-- ============================================
-- TABLE: Sessions
-- ============================================
CREATE TABLE Sessions (
    SessionID   INT IDENTITY(1,1) PRIMARY KEY,
    SessionName NVARCHAR(100) NOT NULL,
    SessionDate DATE          NOT NULL,
    SpecialtyID INT           NOT NULL,
    CourseYear  INT           NOT NULL CHECK (CourseYear BETWEEN 1 AND 5),
    CONSTRAINT FK_Sessions_Specialties FOREIGN KEY (SpecialtyID)
        REFERENCES Specialties(SpecialtyID)
        ON DELETE CASCADE
);
GO

-- ============================================
-- TABLE: Grades (central connecting table)
-- ============================================
CREATE TABLE Grades (
    GradeID    INT IDENTITY(1,1) PRIMARY KEY,
    StudentID  INT           NOT NULL,
    SubjectID  INT           NOT NULL,
    WorkTypeID INT           NOT NULL,
    SessionID  INT           NOT NULL,
    Score      DECIMAL(5,2)  NOT NULL CHECK (Score BETWEEN 0 AND 100),
    GradeDate  DATE          NOT NULL,
    CONSTRAINT FK_Grades_Students  FOREIGN KEY (StudentID)  REFERENCES Students(StudentID),
    CONSTRAINT FK_Grades_Subjects  FOREIGN KEY (SubjectID)  REFERENCES Subjects(SubjectID),
    CONSTRAINT FK_Grades_WorkTypes FOREIGN KEY (WorkTypeID) REFERENCES WorkTypes(WorkTypeID),
    CONSTRAINT FK_Grades_Sessions  FOREIGN KEY (SessionID)  REFERENCES Sessions(SessionID)
);
GO

-- ============================================
-- INSERT: Faculties
-- ============================================
INSERT INTO Faculties (FacultyName) VALUES
    ('Faculty of Science and Technology'),
    ('Faculty of Business and Economics'),
    ('Faculty of Arts and Humanities');
GO

-- ============================================
-- INSERT: Specialties
-- ============================================
INSERT INTO Specialties (SpecialtyName, FacultyID) VALUES
    ('Computer Science',        1),
    ('Software Engineering',    1),
    ('Accounting',              2),
    ('Business Administration', 2);
GO

-- ============================================
-- INSERT: WorkTypes
-- ============================================
INSERT INTO WorkTypes (WorkTypeName) VALUES
    ('Exam'),
    ('Test'),
    ('Term Paper');
GO

-- ============================================
-- INSERT: Students (9 students with updated details)
-- ============================================
INSERT INTO Students (FirstName, LastName, Age, Gender, CourseYear, SpecialtyID) VALUES
    ('Tharindu', 'Silva',        21, 'Male',   3, 1),
    ('Kasun',    'Perera',       20, 'Male',   2, 1),
    ('Nuwan',    'Fernando',     22, 'Male',   3, 2),
    ('Piumi',    'Jayasinghe',   23, 'Female', 4, 2),
    ('Kavindu',  'Rashmika',     24, 'Male',   5, 3),
    ('Sandun',   'Bandara',      23, 'Male',   4, 3),
    ('Chamath',  'Gunawardena',  19, 'Male',   1, 4),
    ('Anuradha', 'Herath',       22, 'Male',   3, 4),
    ('Nimesha',  'Perera',       19, 'Female', 1, 2);
GO

-- ============================================
-- INSERT: Subjects
-- ============================================
INSERT INTO Subjects (SubjectName, SpecialtyID) VALUES
    ('Algorithms & Data Structures', 1),
    ('Database Systems',             1),
    ('Operating Systems',            1),
    ('Software Design Patterns',     2),
    ('Web Development',              2),
    ('Software Testing',             2),
    ('Financial Accounting',         3),
    ('Cost Accounting',              3),
    ('Taxation',                     3),
    ('Business Strategy',            4),
    ('Marketing Management',         4),
    ('Organizational Behavior',      4);
GO

-- ============================================
-- INSERT: Sessions
-- ============================================
INSERT INTO Sessions (SessionName, SessionDate, SpecialtyID, CourseYear) VALUES
    ('Winter Session 2024', '2024-01-15', 1, 3),
    ('Winter Session 2024', '2024-01-15', 2, 3),
    ('Winter Session 2024', '2024-01-15', 3, 5),
    ('Winter Session 2024', '2024-01-15', 4, 3),
    ('Summer Session 2024', '2024-06-10', 1, 2),
    ('Summer Session 2024', '2024-06-10', 2, 4);
GO

-- ============================================
-- INSERT: Grades
-- ============================================
INSERT INTO Grades (StudentID, SubjectID, WorkTypeID, SessionID, Score, GradeDate) VALUES
-- Tharindu Silva (ID=1, Computer Science)
(1, 1, 1, 1, 87.00, '2024-01-20'),
(1, 1, 2, 1, 79.00, '2024-01-10'),
(1, 2, 1, 1, 92.00, '2024-01-22'),
(1, 2, 3, 1, 85.00, '2024-01-25'),
(1, 3, 1, 1, 74.00, '2024-01-24'),

-- Kasun Perera (ID=2, Computer Science)
(2, 1, 2, 5, 68.00, '2024-06-12'),
(2, 2, 1, 5, 72.00, '2024-06-15'),
(2, 3, 2, 5, 58.00, '2024-06-13'),

-- Nuwan Fernando (ID=3, Software Engineering)
(3, 4, 1, 2, 90.00, '2024-01-21'),
(3, 5, 1, 2, 86.00, '2024-01-23'),
(3, 6, 3, 2, 94.00, '2024-01-26'),

-- Piumi Jayasinghe (ID=4, Software Engineering)
(4, 4, 1, 6, 78.00, '2024-06-11'),
(4, 5, 2, 6, 84.00, '2024-06-14'),
(4, 6, 1, 6, 81.00, '2024-06-16'),

-- Kavindu Rashmika (ID=5, Accounting)
(5, 7, 1, 3, 95.00, '2024-01-20'),
(5, 8, 1, 3, 89.00, '2024-01-22'),
(5, 9, 3, 3, 82.00, '2024-01-27'),

-- Sandun Bandara (ID=6, Accounting)
(6, 7, 2, 3, 62.00, '2024-01-19'),
(6, 8, 1, 3, 58.00, '2024-01-21'),
(6, 9, 1, 3, 50.00, '2024-01-23'),

-- Chamath Gunawardena (ID=7, Business Administration)
(7, 10, 2, 4, 75.00, '2024-01-20'),
(7, 11, 1, 4, 70.00, '2024-01-22'),
(7, 12, 2, 4, 73.00, '2024-01-24'),

-- Anuradha Herath (ID=8, Business Administration)
(8, 10, 1, 4, 86.00, '2024-01-21'),
(8, 11, 3, 4, 93.00, '2024-01-25'),
(8, 12, 1, 4, 87.00, '2024-01-23'),

-- Nimesha Perera (ID=9, Software Engineering)
(9, 4, 2, 2, 82.00, '2024-01-18'),
(9, 5, 2, 2, 76.00, '2024-01-20'),
(9, 6, 2, 2, 71.00, '2024-01-22');
GO

-- ============================================
-- VIEWS
-- ============================================

-- Full student info
CREATE VIEW vw_StudentDetails AS
SELECT
    s.StudentID,
    s.FirstName + ' ' + s.LastName AS FullName,
    s.Age, s.Gender, s.CourseYear,
    sp.SpecialtyName,
    f.FacultyName
FROM Students s
JOIN Specialties sp ON s.SpecialtyID = sp.SpecialtyID
JOIN Faculties   f  ON sp.FacultyID  = f.FacultyID;
GO

-- Average score per student
CREATE VIEW vw_StudentAvgScore AS
SELECT
    s.StudentID,
    s.FirstName + ' ' + s.LastName AS FullName,
    sp.SpecialtyName,
    f.FacultyName,
    s.CourseYear,
    ROUND(AVG(g.Score), 2) AS AvgScore
FROM Students    s
JOIN Specialties sp ON s.SpecialtyID = sp.SpecialtyID
JOIN Faculties   f  ON sp.FacultyID  = f.FacultyID
JOIN Grades      g  ON s.StudentID   = g.StudentID
GROUP BY s.StudentID, s.FirstName, s.LastName, sp.SpecialtyName, f.FacultyName, s.CourseYear;
GO

-- Subject difficulty per specialty
CREATE VIEW vw_SubjectDifficulty AS
SELECT
    sub.SubjectID,
    sub.SubjectName,
    sp.SpecialtyName,
    ROUND(AVG(g.Score), 2) AS AvgScore
FROM Subjects    sub
JOIN Specialties sp ON sub.SpecialtyID = sp.SpecialtyID
JOIN Grades       g ON sub.SubjectID   = g.SubjectID
GROUP BY sub.SubjectID, sub.SubjectName, sp.SpecialtyName;
GO

-- ============================================
-- QUICK CHECK QUERIES
-- ============================================
SELECT * FROM vw_StudentDetails;
SELECT * FROM vw_StudentAvgScore ORDER BY AvgScore DESC;
SELECT * FROM vw_SubjectDifficulty ORDER BY SpecialtyName, AvgScore ASC;

PRINT 'UniversityDB created and populated successfully!';
GO