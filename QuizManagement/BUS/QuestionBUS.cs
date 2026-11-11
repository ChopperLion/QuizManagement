using System.Data;
using QuizManagement.DAL;

namespace QuizManagement.BUS
{
    class QuestionBUS
    {
        QuestionDAL dal = new QuestionDAL();

        public DataTable GetQuestions()
        {
            return dal.GetQuestions();
        }

        public int InsertQuestion(
            string content,
            string a,
            string b,
            string c,
            string d,
            string correct
        )
        {
            return dal.InsertQuestion(
                content, a, b, c, d, correct
            );
        }

        public int DeleteQuestion(int id)
        {
            return dal.DeleteQuestion(id);
        }
    }
}