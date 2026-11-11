using System.Data;

namespace QuizManagement.DAL
{
    class UserDAL
    {
        Database db = new Database();

        public DataTable Login(string username, string password)
        {
            string query =
                "SELECT * FROM Users WHERE Username='" + username +
                "' AND Password='" + password + "'";

            return db.ExecuteQuery(query);
        }
    }
}