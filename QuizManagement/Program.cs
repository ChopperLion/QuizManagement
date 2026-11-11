using System;
using System.Windows.Forms;
using QuizManagement.BUS;
using QuizManagement.GUI;

namespace QuizManagement
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string pendingLink = args != null && args.Length > 0 && ExamShareLinkService.IsExamLink(args[0]) ? args[0] : "";
            Application.Run(new frmLogin(pendingLink));
        }
    }
}
