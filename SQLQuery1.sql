USE QuizManagement;
GO

/* ============================================================
   QuizManagement schema migration
   - Makes teacher-owned Exams and Questions possible.
   - Safe to run more than once.
   - Existing rows are assigned to the first Teacher account.
   ============================================================ */

/* Existing optional SubjectID column */
IF COL_LENGTH('dbo.Exams', 'SubjectID') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD SubjectID INT NULL;
END
GO

/* Optional points for each question */
IF COL_LENGTH('dbo.Questions', 'Points') IS NULL
BEGIN
    ALTER TABLE dbo.Questions ADD Points DECIMAL(6,2) NULL;
END
GO

/* Owner columns: which teacher created the data */
IF COL_LENGTH('dbo.Exams', 'CreatedByUserID') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD CreatedByUserID INT NULL;
END
GO

/* Exam access, scheduling, and delivery settings */
IF COL_LENGTH('dbo.Exams', 'ExamPassword') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD ExamPassword NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.Exams', 'ExamMode') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD ExamMode NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Exams_ExamMode DEFAULT N'Exam';
END
GO

IF COL_LENGTH('dbo.Exams', 'StartTime') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD StartTime DATETIME NULL;
END
GO

IF COL_LENGTH('dbo.Exams', 'EndTime') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD EndTime DATETIME NULL;
END
GO

IF COL_LENGTH('dbo.Exams', 'MaxAttempts') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD MaxAttempts INT NULL;
END
GO

IF COL_LENGTH('dbo.Exams', 'IsRandomQuestion') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD IsRandomQuestion BIT NOT NULL
        CONSTRAINT DF_Exams_IsRandomQuestion DEFAULT 0;
END

IF COL_LENGTH('dbo.Exams', 'RandomQuestionCount') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD RandomQuestionCount INT NULL;
END

IF COL_LENGTH('dbo.Exams', 'RoomCode') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD RoomCode NVARCHAR(50) NULL;
END

IF COL_LENGTH('dbo.Exams', 'ExamLink') IS NULL
BEGIN
    ALTER TABLE dbo.Exams ADD ExamLink NVARCHAR(255) NULL;
END

IF COL_LENGTH('dbo.Questions', 'QuestionType') IS NULL
BEGIN
    ALTER TABLE dbo.Questions ADD QuestionType NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Questions_QuestionType DEFAULT N'MultipleChoice';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Questions') AND name = 'QuestionContent' AND max_length <> -1)
BEGIN
    ALTER TABLE dbo.Questions ALTER COLUMN QuestionContent NVARCHAR(MAX) NOT NULL;
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Questions') AND name = 'OptionA' AND max_length <> -1)
BEGIN
    ALTER TABLE dbo.Questions ALTER COLUMN OptionA NVARCHAR(MAX) NULL;
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Questions') AND name = 'OptionB' AND max_length <> -1)
BEGIN
    ALTER TABLE dbo.Questions ALTER COLUMN OptionB NVARCHAR(MAX) NULL;
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Questions') AND name = 'OptionC' AND max_length <> -1)
BEGIN
    ALTER TABLE dbo.Questions ALTER COLUMN OptionC NVARCHAR(MAX) NULL;
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Questions') AND name = 'OptionD' AND max_length <> -1)
BEGIN
    ALTER TABLE dbo.Questions ALTER COLUMN OptionD NVARCHAR(MAX) NULL;
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Questions') AND name = 'CorrectAnswer' AND max_length <> -1)
BEGIN
    ALTER TABLE dbo.Questions ALTER COLUMN CorrectAnswer NVARCHAR(MAX) NOT NULL;
END

IF OBJECT_ID('dbo.ExamAttemptAnswers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExamAttemptAnswers
    (
        AttemptAnswerID INT IDENTITY(1,1) PRIMARY KEY,
        ResultID INT NULL,
        UserID INT NOT NULL,
        ExamID INT NOT NULL,
        QuestionID INT NOT NULL,
        QuestionOrder INT NOT NULL,
        QuestionContent NVARCHAR(MAX) NOT NULL,
        OptionA NVARCHAR(MAX) NULL,
        OptionB NVARCHAR(MAX) NULL,
        OptionC NVARCHAR(MAX) NULL,
        OptionD NVARCHAR(MAX) NULL,
        CorrectAnswer NVARCHAR(MAX) NOT NULL,
        UserAnswer NVARCHAR(MAX) NULL,
        IsCorrect BIT NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ExamAttemptAnswers_CreatedAt DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Exams_ExamMode'
      AND parent_object_id = OBJECT_ID('dbo.Exams')
)
BEGIN
    ALTER TABLE dbo.Exams WITH CHECK
    ADD CONSTRAINT CK_Exams_ExamMode CHECK (ExamMode IN (N'Exam', N'Practice'));
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Exams_TimeWindow'
      AND parent_object_id = OBJECT_ID('dbo.Exams')
)
BEGIN
    ALTER TABLE dbo.Exams WITH CHECK
    ADD CONSTRAINT CK_Exams_TimeWindow CHECK (StartTime IS NULL OR EndTime IS NULL OR EndTime > StartTime);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Exams_MaxAttempts'
      AND parent_object_id = OBJECT_ID('dbo.Exams')
)
BEGIN
    ALTER TABLE dbo.Exams WITH CHECK
    ADD CONSTRAINT CK_Exams_MaxAttempts CHECK (MaxAttempts IS NULL OR MaxAttempts > 0);
END
GO

IF COL_LENGTH('dbo.Questions', 'CreatedByUserID') IS NULL
BEGIN
    ALTER TABLE dbo.Questions ADD CreatedByUserID INT NULL;
END
GO

/* Assign old data to the first Teacher account so it remains visible */
DECLARE @DefaultTeacherID INT;

SELECT TOP 1 @DefaultTeacherID = UserID
FROM dbo.Users
WHERE RoleID = 2
ORDER BY UserID;

IF @DefaultTeacherID IS NOT NULL
BEGIN
    UPDATE dbo.Exams
    SET CreatedByUserID = @DefaultTeacherID
    WHERE CreatedByUserID IS NULL;

    UPDATE dbo.Questions
    SET CreatedByUserID = @DefaultTeacherID
    WHERE CreatedByUserID IS NULL;
END
ELSE
BEGIN
    PRINT 'No teacher account found. CreatedByUserID columns were added, but old data was not assigned.';
END
GO

/* Foreign keys */
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Exams_CreatedByUserID'
      AND parent_object_id = OBJECT_ID('dbo.Exams')
)
BEGIN
    ALTER TABLE dbo.Exams WITH CHECK
    ADD CONSTRAINT FK_Exams_CreatedByUserID
    FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Questions_CreatedByUserID'
      AND parent_object_id = OBJECT_ID('dbo.Questions')
)
BEGIN
    ALTER TABLE dbo.Questions WITH CHECK
    ADD CONSTRAINT FK_Questions_CreatedByUserID
    FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID);
END
GO

/* Indexes for teacher filtering */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Exams_CreatedByUserID'
      AND object_id = OBJECT_ID('dbo.Exams')
)
BEGIN
    CREATE INDEX IX_Exams_CreatedByUserID ON dbo.Exams(CreatedByUserID);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Questions_CreatedByUserID'
      AND object_id = OBJECT_ID('dbo.Questions')
)
BEGIN
    CREATE INDEX IX_Questions_CreatedByUserID ON dbo.Questions(CreatedByUserID);
END
GO

/* Quick verification */
SELECT
    'Exams without owner' AS CheckName,
    COUNT(*) AS Total
FROM dbo.Exams
WHERE CreatedByUserID IS NULL
UNION ALL
SELECT
    'Questions without owner' AS CheckName,
    COUNT(*) AS Total
FROM dbo.Questions
WHERE CreatedByUserID IS NULL;
GO
