using System.Data;
using QuizManagement.DAL;

namespace QuizManagement.BUS
{
    class UserBUS
    {
        UserDAL dal = new UserDAL();

        public DataTable Login(string username, string password)
        {
            return dal.Login(username, password);
        }
    }
}