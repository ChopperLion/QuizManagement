using System.Data;

namespace QuizManagement.DAL
{
    class ExamDAL
    {
        Database db = new Database();

        public DataTable GetExams()
        {
            return db.ExecuteQuery(
                "SELECT * FROM Exams"
            );
        }

        public int CreateExam(
            string name,
            int duration
        )
        {
            string query =
                "INSERT INTO Exams VALUES " +
                "(N'" + name + "'," + duration + ")";

            return db.ExecuteNonQuery(query);
        }

        public int AddQuestionToExam(
            int examID,
            int questionID
        )
        {
            string query =
                "INSERT INTO ExamQuestions VALUES " +
                "(" + examID + "," + questionID + ")";

            return db.ExecuteNonQuery(query);
        }
    }
}