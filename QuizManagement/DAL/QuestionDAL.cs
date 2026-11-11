using System.Data;

namespace QuizManagement.DAL
{
    class QuestionDAL
    {
        Database db = new Database();

        // Lấy danh sách câu hỏi
        public DataTable GetQuestions()
        {
            string query = "SELECT * FROM Questions";

            return db.ExecuteQuery(query);
        }

        // Thêm câu hỏi
        public int InsertQuestion(
            string content,
            string a,
            string b,
            string c,
            string d,
            string correct
        )
        {
            Database.EnsureAdvancedExamColumns();
            string query =
                "INSERT INTO Questions (QuestionContent,OptionA,OptionB,OptionC,OptionD,CorrectAnswer,QuestionType) VALUES " +
                "(N'" + content + "',N'" + a + "',N'" + b +
                "',N'" + c + "',N'" + d + "',N'" + correct + "',N'MultipleChoice')";

            return db.ExecuteNonQuery(query);
        }

        // Xóa câu hỏi
        public int DeleteQuestion(int id)
        {
            string query =
                "DELETE FROM Questions WHERE QuestionID=" + id;

            return db.ExecuteNonQuery(query);
        }
    }
}
