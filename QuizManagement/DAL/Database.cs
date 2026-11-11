using System.Data;
using System.Data.SqlClient;

namespace QuizManagement.DAL
{
    class Database
    {
        // Chuỗi kết nối SQL Server
        private string connectionString =
    @"Data Source=.\SQLEXPRESS;
    Initial Catalog=QuizManagement;
    Integrated Security=True";

        // Hàm mở kết nối
        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        // Hàm thực thi SELECT
        public DataTable ExecuteQuery(string query)
        {
            SqlConnection conn = GetConnection();

            SqlDataAdapter da = new SqlDataAdapter(query, conn);

            DataTable dt = new DataTable();

            da.Fill(dt);

            return dt;
        }

        // Hàm INSERT UPDATE DELETE
        public int ExecuteNonQuery(string query)
        {
            SqlConnection conn = GetConnection();

            conn.Open();

            SqlCommand cmd = new SqlCommand(query, conn);

            int result = cmd.ExecuteNonQuery();

            conn.Close();

            return result;
        }

        /// <summary>Thêm cột Points (số điểm câu hỏi, nullable) nếu chưa có.</summary>
        public static void EnsureQuestionPointsColumn()
        {
            try
            {
                var db = new Database();
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'Points') " +
                    "ALTER TABLE Questions ADD Points DECIMAL(6,2) NULL");
            }
            catch
            {
                // Bỏ qua nếu không có quyền DDL — app vẫn chạy với cột đã tồn tại
            }
        }

        public static void EnsureTeacherOwnershipColumns()
        {
            try
            {
                var db = new Database();
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'CreatedByUserID') " +
                    "ALTER TABLE Exams ADD CreatedByUserID INT NULL");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'CreatedByUserID') " +
                    "ALTER TABLE Questions ADD CreatedByUserID INT NULL");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'IsPublishedToServer') " +
                    "ALTER TABLE Questions ADD IsPublishedToServer BIT NOT NULL CONSTRAINT DF_Questions_IsPublishedToServer DEFAULT 0");
                db.ExecuteNonQuery(
                    "DECLARE @DefaultTeacherID INT; " +
                    "SELECT TOP 1 @DefaultTeacherID = UserID FROM Users WHERE RoleID = 2 ORDER BY UserID; " +
                    "IF @DefaultTeacherID IS NOT NULL BEGIN " +
                    "UPDATE Exams SET CreatedByUserID = @DefaultTeacherID WHERE CreatedByUserID IS NULL; " +
                    "UPDATE Questions SET CreatedByUserID = @DefaultTeacherID WHERE CreatedByUserID IS NULL; " +
                    "END");
            }
            catch
            {
                // App can still start; screens that need ownership will require the SQL migration.
            }
        }

        public static void EnsureExamAccessColumns()
        {
            try
            {
                var db = new Database();
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'ExamPassword') " +
                    "ALTER TABLE Exams ADD ExamPassword NVARCHAR(100) NULL");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'ExamMode') " +
                    "ALTER TABLE Exams ADD ExamMode NVARCHAR(20) NOT NULL CONSTRAINT DF_Exams_ExamMode DEFAULT N'Exam'");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'StartTime') " +
                    "ALTER TABLE Exams ADD StartTime DATETIME NULL");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'EndTime') " +
                    "ALTER TABLE Exams ADD EndTime DATETIME NULL");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'MaxAttempts') " +
                    "ALTER TABLE Exams ADD MaxAttempts INT NULL");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'IsRandomQuestion') " +
                    "ALTER TABLE Exams ADD IsRandomQuestion BIT NOT NULL CONSTRAINT DF_Exams_IsRandomQuestion DEFAULT 0");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'RandomQuestionCount') " +
                    "ALTER TABLE Exams ADD RandomQuestionCount INT NULL");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'RoomCode') " +
                    "ALTER TABLE Exams ADD RoomCode NVARCHAR(50) NULL");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'ExamLink') " +
                    "ALTER TABLE Exams ADD ExamLink NVARCHAR(255) NULL");
            }
            catch
            {
                // App can still start; run SQLQuery1.sql if the login cannot alter schema.
            }
        }

        public static void EnsureAdvancedExamColumns()
        {
            try
            {
                var db = new Database();
                EnsureQuestionTextColumns();
                EnsurePracticeExamColumns();
                EnsureExamQuestionPointsColumn();
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'QuestionType') " +
                    "ALTER TABLE Questions ADD QuestionType NVARCHAR(20) NOT NULL CONSTRAINT DF_Questions_QuestionType DEFAULT N'MultipleChoice'");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'ExamAttemptAnswers') AND type in (N'U')) " +
                    "CREATE TABLE ExamAttemptAnswers (" +
                    "AttemptAnswerID INT IDENTITY(1,1) PRIMARY KEY," +
                    "ResultID INT NULL, UserID INT NOT NULL, ExamID INT NOT NULL, QuestionID INT NOT NULL," +
                    "QuestionOrder INT NOT NULL, QuestionContent NVARCHAR(MAX) NOT NULL," +
                    "OptionA NVARCHAR(MAX) NULL, OptionB NVARCHAR(MAX) NULL, OptionC NVARCHAR(MAX) NULL, OptionD NVARCHAR(MAX) NULL," +
                    "CorrectAnswer NVARCHAR(MAX) NOT NULL, UserAnswer NVARCHAR(MAX) NULL, IsCorrect BIT NOT NULL," +
                    "QuestionPoints DECIMAL(6,2) NULL, AwardedPoints DECIMAL(6,2) NULL, CreatedAt DATETIME NOT NULL DEFAULT GETDATE())");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ExamAttemptAnswers') AND name = 'QuestionPoints') " +
                    "ALTER TABLE ExamAttemptAnswers ADD QuestionPoints DECIMAL(6,2) NULL");
                db.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ExamAttemptAnswers') AND name = 'AwardedPoints') " +
                    "ALTER TABLE ExamAttemptAnswers ADD AwardedPoints DECIMAL(6,2) NULL");
            }
            catch
            {
                // App can still run; review/export history will need the migration when DB permissions allow it.
            }
        }

        public static void EnsurePracticeExamColumns()
        {
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'ScoreScale') " +
                "ALTER TABLE Exams ADD ScoreScale DECIMAL(6,2) NOT NULL CONSTRAINT DF_Exams_ScoreScale DEFAULT 10");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'PracticeMaxWrongPerQuestion') " +
                "ALTER TABLE Exams ADD PracticeMaxWrongPerQuestion INT NULL");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'PracticeShowAnswer') " +
                "ALTER TABLE Exams ADD PracticeShowAnswer BIT NOT NULL CONSTRAINT DF_Exams_PracticeShowAnswer DEFAULT 1");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'PracticeShowPointGain') " +
                "ALTER TABLE Exams ADD PracticeShowPointGain BIT NOT NULL CONSTRAINT DF_Exams_PracticeShowPointGain DEFAULT 1");            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'PracticeAutoNextOnCorrect') " +
                "ALTER TABLE Exams ADD PracticeAutoNextOnCorrect BIT NOT NULL CONSTRAINT DF_Exams_PracticeAutoNextOnCorrect DEFAULT 0");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'IsPublishedToServer') " +
                "ALTER TABLE Exams ADD IsPublishedToServer BIT NOT NULL CONSTRAINT DF_Exams_IsPublishedToServer DEFAULT 0");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'RequireClassBeforeStart') " +
                "ALTER TABLE Exams ADD RequireClassBeforeStart BIT NOT NULL CONSTRAINT DF_Exams_RequireClassBeforeStart DEFAULT 1");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'AllowStudentEditClass') " +
                "ALTER TABLE Exams ADD AllowStudentEditClass BIT NOT NULL CONSTRAINT DF_Exams_AllowStudentEditClass DEFAULT 1");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'PracticeAllowStudentOptions') " +
                "ALTER TABLE Exams ADD PracticeAllowStudentOptions BIT NOT NULL CONSTRAINT DF_Exams_PracticeAllowStudentOptions DEFAULT 1");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'PracticeNoLoginRequired') " +
                "ALTER TABLE Exams ADD PracticeNoLoginRequired BIT NOT NULL CONSTRAINT DF_Exams_PracticeNoLoginRequired DEFAULT 0");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Exams') AND name = 'PracticeUnlimitedTime') " +
                "ALTER TABLE Exams ADD PracticeUnlimitedTime BIT NOT NULL CONSTRAINT DF_Exams_PracticeUnlimitedTime DEFAULT 0");
        }

        public static void EnsureExamQuestionPointsColumn()
        {
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ExamQuestions') AND name = 'Points') " +
                "ALTER TABLE ExamQuestions ADD Points DECIMAL(6,2) NULL");
            ExecuteSchemaChange(
                "UPDATE eq SET eq.Points = ISNULL(q.Points, 1) " +
                "FROM ExamQuestions eq INNER JOIN Questions q ON eq.QuestionID = q.QuestionID " +
                "WHERE eq.Points IS NULL");
        }

        public static void EnsureStudentAssessmentColumns()
        {
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'ClassName') " +
                "ALTER TABLE Users ADD ClassName NVARCHAR(100) NULL");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'AvatarPath') " +
                "ALTER TABLE Users ADD AvatarPath NVARCHAR(500) NULL");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Results') AND name = 'StudentFullName') " +
                "ALTER TABLE Results ADD StudentFullName NVARCHAR(200) NULL");
            ExecuteSchemaChange(
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Results') AND name = 'StudentClass') " +
                "ALTER TABLE Results ADD StudentClass NVARCHAR(100) NULL");
        }

        public static void EnsureQuestionTextColumns()
        {
            ExecuteSchemaChange(
                "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'QuestionContent' AND max_length <> -1) " +
                "ALTER TABLE Questions ALTER COLUMN QuestionContent NVARCHAR(MAX) NOT NULL");
            ExecuteSchemaChange(
                "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'OptionA' AND max_length <> -1) " +
                "ALTER TABLE Questions ALTER COLUMN OptionA NVARCHAR(MAX) NULL");
            ExecuteSchemaChange(
                "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'OptionB' AND max_length <> -1) " +
                "ALTER TABLE Questions ALTER COLUMN OptionB NVARCHAR(MAX) NULL");
            ExecuteSchemaChange(
                "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'OptionC' AND max_length <> -1) " +
                "ALTER TABLE Questions ALTER COLUMN OptionC NVARCHAR(MAX) NULL");
            ExecuteSchemaChange(
                "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'OptionD' AND max_length <> -1) " +
                "ALTER TABLE Questions ALTER COLUMN OptionD NVARCHAR(MAX) NULL");
            ExecuteSchemaChange(
                "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Questions') AND name = 'CorrectAnswer' AND max_length <> -1) " +
                "ALTER TABLE Questions ALTER COLUMN CorrectAnswer NVARCHAR(MAX) NOT NULL");
        }

        private static void ExecuteSchemaChange(string sql)
        {
            try
            {
                new Database().ExecuteNonQuery(sql);
            }
            catch
            {
                // App can continue; import will surface a clear SQL error if a required migration is blocked.
            }
        }
    }
}



