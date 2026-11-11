using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using QuizManagement.DAL;

namespace QuizManagement.BUS
{
    public class StudentExamWebServer
    {
        public const int DefaultPort = 5000;
        public const int MaxAutoPort = 5010;
        private static readonly object SyncRoot = new object();
        private static StudentExamWebServer instance;
        private TcpListener listener;
        private Thread serverThread;
        private volatile bool running;
        private int currentPort = DefaultPort;

        public static StudentExamWebServer EnsureStarted()
        {
            lock (SyncRoot)
            {
                if (instance == null)
                {
                    instance = new StudentExamWebServer();
                    instance.Start();
                }
                return instance;
            }
        }

        public static int GetCurrentPort() { return EnsureStarted().currentPort; }
        public static string GetBaseUrl() { return "http://" + GetLocalIpAddress() + ":" + GetCurrentPort(); }
        public static string BuildExamUrl(string roomCode) { return GetBaseUrl() + "/exam/" + Uri.EscapeDataString(roomCode ?? ""); }

        private void Start()
        {
            if (running) return;
            Exception lastError = null;
            for (int port = DefaultPort; port <= MaxAutoPort; port++)
            {
                try
                {
                    listener = new TcpListener(IPAddress.Any, port);
                    listener.Start();
                    currentPort = port;
                    running = true;
                    serverThread = new Thread(AcceptLoop) { IsBackground = true, Name = "StudentExamWebServer" };
                    serverThread.Start();
                    return;
                }
                catch (Exception ex) { lastError = ex; }
            }
            throw new InvalidOperationException("Khong the khoi dong web server lam bai.", lastError);
        }

        private void AcceptLoop()
        {
            while (running)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
                }
                catch { if (!running) return; }
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    var request = ReadRequest(client);
                    if (request == null) return;
                    WriteResponse(client, Dispatch(request));
                }
                catch (Exception ex)
                {
                    WriteResponse(client, ErrorPage(V("Co loi xay ra"), Enc(ex.Message)));
                }
            }
        }

        private string Dispatch(HttpRequest request)
        {
            var path = request.Path.Trim('/');
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !Eq(parts[0], "exam")) return ErrorPage(V("Khong tim thay trang"), "");
            var roomCode = Uri.UnescapeDataString(parts[1]);
            if (parts.Length >= 3 && Eq(parts[2], "ping")) return Plain("ok");
            if (request.Method == "POST" && parts.Length >= 3 && Eq(parts[2], "login")) return LoginAndShowExam(roomCode, ParseForm(request.Body));
            if (request.Method == "POST" && parts.Length >= 3 && Eq(parts[2], "submit")) return SubmitExam(roomCode, ParseForm(request.Body));
            return ShowEntry(roomCode, "");
        }

        private string ShowEntry(string roomCode, string message)
        {
            var exam = FindExam(roomCode);
            if (exam == null) return ErrorPage(V("Khong tim thay bai thi"), V("Ma phong hoac lien ket bai thi khong hop le."));
            var title = S(exam, "ExamName");
            bool isPractice = Eq(S(exam, "ExamMode"), "Practice");
            bool noLoginRequired = isPractice && B(exam, "PracticeNoLoginRequired", false);
            bool unlimitedTime = isPractice && B(exam, "PracticeUnlimitedTime", false);
            var error = string.IsNullOrEmpty(message) ? "" : "<div class='msg error'>" + T(message) + "</div>";
            var timing = unlimitedTime
                ? T(V("Thoi gian khong gioi han"))
                : T(V("Thoi gian lam bai")) + ": " + I(exam, "Duration", 0) + " " + T(V("phut"));
            if (noLoginRequired)
            {
                var guest = GetOrCreateGuestUserRow();
                return BuildExamWorkspace(roomCode, exam, guest, error, timing);
            }
            return Layout(title, @"
  <main class='auth-wrap'><section class='auth-card auth-card--login'>
  <span class='pill'>" + T(V("Bai thi truc tuyen")) + @"</span>
  <h1>" + Enc(title) + @"</h1>
  <p class='muted'>" + timing + @"</p>" + error + @"
  <form method='post' action='/exam/" + Enc(Uri.EscapeDataString(roomCode)) + @"/login' accept-charset='utf-8'>
  <label>" + T(V("Ten dang nhap")) + @"</label><input name='username' autocomplete='username' />
  <label>" + T(V("Mat khau tai khoan")) + @"</label><input name='password' type='password' autocomplete='current-password' />
  <label>" + T(V("Mat khau bai thi")) + @"</label><input name='examPassword' type='password' />
  <button class='btn primary' type='submit'>" + T(V("Vao lam bai")) + @"</button>
  </form></section></main>");
        }
        private string LoginAndShowExam(string roomCode, Dictionary<string, string> form)
        {
            var exam = FindExam(roomCode);
            if (exam == null) return ErrorPage(V("Khong tim thay bai thi"), "");
            var examPassword = S(exam, "ExamPassword");
            if (!string.IsNullOrEmpty(examPassword) && Get(form, "examPassword") != examPassword) return ShowEntry(roomCode, V("Mat khau bai thi khong dung."));
            var user = LoginStudent(Get(form, "username"), Get(form, "password"));
            if (user == null) return ShowEntry(roomCode, V("Ten dang nhap hoac mat khau khong dung."));
            var reason = CanOpenExam(exam);
            if (!string.IsNullOrEmpty(reason)) return ErrorPage(V("Chua the vao bai thi"), reason);
            return BuildExamWorkspace(roomCode, exam, user, "", "");
        }
        private string BuildExamWorkspace(string roomCode, DataRow exam, DataRow user, string entryMessageHtml, string entryTimingText)
        {
            int examId = I(exam, "ExamID", 0);
            var questions = LoadQuestions(exam);
            var title = S(exam, "ExamName");
            bool isPractice = Eq(S(exam, "ExamMode"), "Practice");
            bool allowOptions = B(exam, "PracticeAllowStudentOptions", true);
            bool noLoginRequired = isPractice && B(exam, "PracticeNoLoginRequired", false);
            bool unlimitedTime = isPractice && B(exam, "PracticeUnlimitedTime", false);
            bool showAnswer = B(exam, "PracticeShowAnswer", true);
            bool showPointGain = B(exam, "PracticeShowPointGain", true);
            bool autoNext = B(exam, "PracticeAutoNextOnCorrect", false);
            bool requireClass = B(exam, "RequireClassBeforeStart", true);
            bool allowEditClass = B(exam, "AllowStudentEditClass", true);
            int maxWrong = I(exam, "PracticeMaxWrongPerQuestion", 0);
            int duration = I(exam, "Duration", 0);
            int clientDuration = unlimitedTime ? 52560000 : duration;
            decimal scoreScale = D(exam, "ScoreScale", 10);
            string studentName = First(S(user, "FullName"), S(user, "Username"));
            string className = S(user, "ClassName");
            decimal totalRawPoints = questions.Sum(q => D(q, "Points", 1));
            if (totalRawPoints <= 0) totalRawPoints = questions.Count;
            decimal pointScaleFactor = totalRawPoints > 0 ? scoreScale / totalRawPoints : 1M;

            var ids = string.Join(",", questions.Select(q => I(q, "QuestionID", 0)));
            var entryMessage = string.IsNullOrEmpty(entryMessageHtml) ? "" : entryMessageHtml;
            string timingText = string.IsNullOrWhiteSpace(entryTimingText)
                ? (unlimitedTime ? T(V("Thoi gian khong gioi han")) : T(V("Thoi gian lam bai")) + ": " + duration + " " + T(V("phut")))
                : entryTimingText;

            var startPanel = @"
    <section id='startPanel' class='card start-panel'>
      <div class='start-hero'><div><span class='pill pill-soft'>" + T(noLoginRequired ? V("Vao on khong can dang nhap") : V("Xac nhan thong tin truoc khi vao bai")) + @"</span><h2>" + T(V("Thong tin du thi")) + @"</h2><p class='muted'>" + timingText + @"</p></div><div class='start-highlight'>" + T(isPractice ? V("Co the doi tuy chon on tap truoc khi bat dau.") : V("Kiem tra lai thong tin roi bat dau lam bai.")) + @"</div></div>" + entryMessage + @"
      <div class='grid2'><div><h2>" + T(V("Thong tin du thi")) + @"</h2>
      <label>" + T(V("Ho ten")) + @"</label><input name='studentFullName' value='" + Enc(studentName) + @"' /></div>
      <div><h2>" + T(V("Nhap dung ho ten va lop de giao vien tong hop diem")) + @"</h2>
      <label>" + T(V("Lop")) + @"</label><input name='studentClass' value='" + Enc(className) + @"' " + (allowEditClass ? "" : "readonly") + @" /></div></div>" +
      (isPractice && allowOptions ? PracticeOptions(maxWrong, showAnswer, showPointGain, autoNext) : "") + @"
      <label class='checkline'><input id='confirmStart' type='checkbox' /> <strong>" + T(V("Xac nhan thong tin da nhap")) + @"</strong><span>" + T(requireClass ? V("Phai co lop truoc khi bat dau.") : V("Kiem tra thong tin truoc khi bat dau.")) + @"</span></label>
      <div class='actions right'><button id='btnStart' class='btn primary' type='button'>" + T(V("Bat dau lam bai")) + @"</button></div>
    </section>";

            var html = @"
<main class='exam-shell'>
  <section class='exam-main'>
    <div class='exam-hero'><div><span class='pill'>" + T(isPractice ? V("On luyen tren trinh duyet") : V("Lam bai tren trinh duyet")) + @"</span><h1>" + Enc(title) + @"</h1><p class='muted'>" + T(V("Sinh vien")) + @": " + Enc(studentName) + @"</p></div>
    <div class='timer " + (unlimitedTime ? "timer-unlimited" : "") + "'>" + (unlimitedTime ? "<span>" + T(V("Thoi gian")) + "</span><strong id='timer'>" + T(V("Khong gioi han")) + "</strong><small>" + T(V("Ban co the on tap thoai mai, khong bi dem nguoc.")) + "</small>" : "<span>" + T(V("Thoi gian con lai")) + "</span><strong id='timer'>" + duration + ":00</strong><small>" + T(V("Can canh nhip nho de giu tien do lam bai.")) + "</small>") + @"</div></div>
    <form id='examForm' method='post' action='/exam/" + Enc(Uri.EscapeDataString(roomCode)) + @"/submit' accept-charset='utf-8'>
      <input type='hidden' name='userId' value='" + I(user, "UserID", 0) + @"' /><input type='hidden' name='examId' value='" + examId + @"' />
      <input type='hidden' name='questionIds' value='" + Enc(ids) + @"' /><input type='hidden' name='earnedJson' id='earnedJson' /><input type='hidden' name='answersJson' id='answersJson' />
      " + startPanel + @"
      <div class='exam-progress' style='display:none'><strong id='questionCounter'>" + T(V("Cau")) + @" 1/" + questions.Count + @"</strong><strong id='answeredCounter'>0/" + questions.Count + @" " + T(V("cau da tra loi")) + @"</strong></div>
      <section id='questionHost' class='card question-card' style='display:none'></section>
      <div class='actions nav-actions' style='display:none'><div class='nav-actions-card'><button class='btn ghost nav-btn' id='btnPrev' type='button'>" + T(V("Cau truoc")) + @"</button><button class='btn ghost nav-btn' id='btnClear' type='button'>" + T(V("Xoa dap an")) + @"</button><button class='btn ghost nav-btn' id='btnNext' type='button'>" + T(V("Cau tiep")) + @"</button></div><button class='btn primary submit-btn' id='btnSubmit' type='button'>" + T(V("Nop bai")) + @"</button></div>
    </form>
  </section>
  <aside class='quick-nav card' style='display:none'><h2>" + T(V("Dieu huong nhanh")) + @"</h2><p class='muted'>" + T(V("Bam vao so cau de chuyen nhanh. Mau xanh la da tra loi.")) + @"</p><div id='navGrid' class='nav-grid'></div><div class='score-box'>" + T(V("Diem cong don hien tai")) + @": <span id='practiceScore'>0</span></div><div class='legend'><span><i class='dot active'></i>" + T(V("Dang lam")) + @"</span><span><i class='dot answered'></i>" + T(V("Da tra loi")) + @"</span><span><i class='dot'></i>" + T(V("Chua tra loi")) + @"</span></div></aside>
</main>
<script>window.examData={questions:" + BuildQuestionsJson(questions) + @",duration:" + clientDuration + @",scoreScale:" + scoreScale.ToString(CultureInfo.InvariantCulture) + @",isPractice:" + JsBool(isPractice) + @",showAnswer:" + JsBool(showAnswer) + @",showPointGain:" + JsBool(showPointGain) + @",autoNext:" + JsBool(autoNext) + @",requireClass:" + JsBool(requireClass) + @",maxWrong:" + maxWrong + @",totalRawPoints:" + totalRawPoints.ToString(CultureInfo.InvariantCulture) + @",pointScaleFactor:" + pointScaleFactor.ToString(CultureInfo.InvariantCulture) + @",unlimitedTime:" + JsBool(unlimitedTime) + @"};</script>" + BuildUnlimitedTimeScript(true) + BuildClientScript() + BuildUnlimitedTimeScript(false);
            return Layout(title, html);
        }

        private string PracticeOptions(int maxWrong, bool showAnswer, bool showPointGain, bool autoNext)
        {
            return @"<div class='practice-options'><h3>" + T(V("Tuy chon on do nguoi lam bai tu chon")) + @"</h3>
<label>" + T(V("Sai toi da moi cau")) + @"<input id='optMaxWrong' type='number' min='0' value='" + maxWrong + @"' /></label>
<label class='checkline compact'><input id='optShowAnswer' type='checkbox' " + (showAnswer ? "checked" : "") + @" /> " + T(V("Hien dap an")) + @"</label>
<label class='checkline compact'><input id='optShowPointGain' type='checkbox' " + (showPointGain ? "checked" : "") + @" /> " + T(V("Hien diem cong don")) + @"</label>
<label class='checkline compact'><input id='optAutoNext' type='checkbox' " + (autoNext ? "checked" : "") + @" /> " + T(V("Tu chuyen cau khi dung")) + @"</label></div>";
        }

        private DataRow GetOrCreateGuestUserRow()
        {
            var db = new Database();
            const string guestUsername = "web_practice_guest";
            var dt = db.ExecuteQuery("SELECT TOP 1 * FROM Users WHERE Username=N'" + Sql(guestUsername) + "' ORDER BY UserID");
            if (dt.Rows.Count == 0)
            {
                db.ExecuteNonQuery("INSERT INTO Users (Username, Password, FullName, RoleID, ClassName) VALUES (N'" + Sql(guestUsername) + "',N'',N'KhachOnTap',3,NULL)");
                dt = db.ExecuteQuery("SELECT TOP 1 * FROM Users WHERE Username=N'" + Sql(guestUsername) + "' ORDER BY UserID");
            }
            if (dt.Rows.Count > 0) return dt.Rows[0];

            var table = new DataTable();
            table.Columns.Add("UserID", typeof(int));
            table.Columns.Add("Username", typeof(string));
            table.Columns.Add("FullName", typeof(string));
            table.Columns.Add("ClassName", typeof(string));
            var row = table.NewRow();
            row["UserID"] = 1;
            row["Username"] = guestUsername;
            row["FullName"] = "KhachOnTap";
            row["ClassName"] = "";
            table.Rows.Add(row);
            return row;
        }

        private string BuildUnlimitedTimeScript(bool beforeClientScript)
        {
            if (beforeClientScript)
            {
                return @"<script>(function(){if(!window.examData||!window.examData.unlimitedTime)return;var originalSetInterval=window.setInterval;window.setInterval=function(fn,delay){if(delay===1000)return 0;return originalSetInterval.apply(window,arguments);};})();</script>";
            }

            return "<script>(function(){if(!window.examData||!window.examData.unlimitedTime)return;var labelText=" + Js(V("Thoi gian")) + ";var timerText=" + Js(V("Khong gioi han")) + ";function sync(){var timer=document.getElementById('timer');var label=document.querySelector('.timer span');if(label)label.textContent=labelText;if(timer)timer.textContent=timerText;}sync();setInterval(sync,500);})();</script>";
        }

        private string BuildClientScript()
        {
            return "<script>" + Encoding.UTF8.GetString(Convert.FromBase64String("Y29uc3QgdHh0PXtxOidDw6J1JyxhbnN3ZXJlZDonY8OidSDEkcOjIHRy4bqjIGzhu51pJyxlbnRlckFuc3dlcjonTmjhuq1wIMSRw6FwIMOhbiBj4bunYSBi4bqhbicsY2hlY2s6J0tp4buDbSB0cmEgxJHDoXAgw6FuJyxjb3JyZWN0OidDaOG7jW4gxJHDum5nIHLhu5NpJyxjb3JyZWN0UmV0cnk6J0LhuqFuIMSRw6MgY2jhu41uIMSRw7puZyBzYXUga2hpIHRo4butIGzhuqFpIHbDoCB24bqrbiDEkcaw4bujYyB0w61uaCDEkWnhu4NtLicsd3JvbmdSZXRyeTonU2FpIHLDuWksIGLhuqFuIGPDsm4gdOG7kWkgxJFhIHtufSBs4bqnbiBjaOG7jW4gbOG6oWkuJyx3cm9uZ0xvY2tlZDonQuG6oW4gxJHDoyBkw7luZyBo4bq/dCBz4buRIGzhuqduIGNo4buNbiBs4bqhaSwgY8OidSBuw6B5IGtow7RuZyDEkcaw4bujYyB0w61uaCDEkWnhu4NtLicsZmlsbEVtcHR5OidIw6N5IG5o4bqtcCDEkcOhcCDDoW4gdHLGsOG7m2Mga2hpIGtp4buDbSB0cmEuJyxmaWxsUmV0cnk6J0NoxrBhIGto4bubcCBs4bqvbSwgYuG6oW4gY8OybiB04buRaSDEkWEge259IGzhuqduIG5o4bqtcCBs4bqhaS4nLGZpbGxMb2NrZWQ6J0LhuqFuIMSRw6MgZMO5bmcgaOG6v3Qgc+G7kSBs4bqnbiBuaOG6rXAgbOG6oWksIGPDonUgbsOgeSBraMO0bmcgxJHGsOG7o2MgdMOtbmggxJFp4buDbS4nLG5lYXI6J0PDonUgdHLhuqMgbOG7nWkgZ+G6p24gxJHDum5nLCB04bqhbSBj4buZbmcge2dhaW59L3t0b3RhbH0gxJFp4buDbS4nLGFuc3dlcjonxJDDoXAgw6FuIMSRw7puZycscG9pbnQ6J8SQaeG7g20gY+G7mW5nIGThuqduJyxuZWVkQ2xhc3M6J0LhuqFuIGPhuqduIG5o4bqtcCBs4bubcCB0csaw4bubYyBraGkgYuG6r3QgxJHhuqd1LicsbmVlZENvbmZpcm06J0LhuqFuIGPhuqduIHjDoWMgbmjhuq1uIHRow7RuZyB0aW4gdHLGsOG7m2Mga2hpIGLhuq90IMSR4bqndS4nLHN1Ym1pdDonQuG6oW4gY2jhuq9jIGNo4bqvbiBtdeG7kW4gbuG7mXAgYsOgaT8nLGxlYXZlOidC4bqhbiDEkWFuZyBsw6BtIGLDoGkuIE7hur91IHLhu51pIHRyYW5nIGzDumMgbsOgeSwgYsOgaSBjw7MgdGjhu4MgYuG7iyBu4buZcCB24bubaSBjw6FjIMSRw6FwIMOhbiBoaeG7h24gY8OzLicsdGltZVVwOidI4bq/dCBnaeG7nSBsw6BtIGLDoGkuIEjhu4cgdGjhu5FuZyBz4bq9IG7hu5lwIGLDoGkgbmdheSBiw6J5IGdp4budLid9Owpjb25zdCBkYXRhPXdpbmRvdy5leGFtRGF0YXx8e307CmNvbnN0IHFzPWRhdGEucXVlc3Rpb25zfHxbXTsKbGV0IGlkeD0wLCBzdGFydGVkPWZhbHNlLCBzdWJtaXR0ZWQ9ZmFsc2UsIGFuc3dlcnM9e30sIGVhcm5lZD17fSwgd3Jvbmc9e30sIGxvY2tlZD17fSwgcHJhY3RpY2VGZWVkYmFjaz17fSwgdGltZXJJZD1udWxsLCByZW1haW5pbmdTZWNvbmRzPShOdW1iZXIoZGF0YS5kdXJhdGlvbil8fDApKjYwOwpjb25zdCAkPWlkPT5kb2N1bWVudC5nZXRFbGVtZW50QnlJZChpZCk7CmZ1bmN0aW9uIGVzYyhzKXtyZXR1cm4gU3RyaW5nKHN8fCcnKS5yZXBsYWNlKC9bJjw+IiddL2csbT0+KHsnJic6JyZhbXA7JywnPCc6JyZsdDsnLCc+JzonJmd0OycsJyInOicmcXVvdDsnLCInIjonJiMzOTsnfVttXSkpO30KZnVuY3Rpb24gZm10KG4pe3JldHVybiAoTWF0aC5yb3VuZCgoTnVtYmVyKG4pfHwwKSoxMDApLzEwMCkudG9TdHJpbmcoKS5yZXBsYWNlKC9cLjAwJC8sJycpO30KZnVuY3Rpb24gdG90YWxQb2ludHMoKXtjb25zdCBzZXJ2ZXJUb3RhbD1OdW1iZXIoZGF0YS50b3RhbFJhd1BvaW50cyl8fDA7IHJldHVybiBzZXJ2ZXJUb3RhbD4wID8gc2VydmVyVG90YWwgOiBxcy5yZWR1Y2UoKGEscSk9PmErKE51bWJlcihxLnBvaW50cyl8fDApLDApO30KZnVuY3Rpb24gdG9EaXNwbGF5KHJhdyl7Y29uc3QgZmFjdG9yPU51bWJlcihkYXRhLnBvaW50U2NhbGVGYWN0b3IpfHwwOyBpZihmYWN0b3I+MCkgcmV0dXJuIChOdW1iZXIocmF3KXx8MCkqZmFjdG9yOyBjb25zdCB0b3RhbD10b3RhbFBvaW50cygpLCBzY2FsZT1OdW1iZXIoZGF0YS5zY29yZVNjYWxlKXx8MTA7IHJldHVybiB0b3RhbD4wID8gKE51bWJlcihyYXcpfHwwKS90b3RhbCpzY2FsZSA6IChOdW1iZXIocmF3KXx8MCk7fQpmdW5jdGlvbiBlYXJuZWRSYXcoKXtyZXR1cm4gT2JqZWN0LnZhbHVlcyhlYXJuZWQpLnJlZHVjZSgoYSxiKT0+YSsoTnVtYmVyKGIpfHwwKSwwKTt9CmZ1bmN0aW9uIGVhcm5lZERpc3BsYXkoKXtyZXR1cm4gZm10KHRvRGlzcGxheShlYXJuZWRSYXcoKSkpO30KZnVuY3Rpb24gYW5zd2VyZWRDb3VudCgpe3JldHVybiBPYmplY3Qua2V5cyhhbnN3ZXJzKS5maWx0ZXIoaz0+U3RyaW5nKGFuc3dlcnNba118fCcnKS50cmltKCkhPT0nJykubGVuZ3RoO30KZnVuY3Rpb24gY3VycmVudE1heFdyb25nKCl7Y29uc3QgZWw9JCgnb3B0TWF4V3JvbmcnKTsgY29uc3Qgdj1lbD9wYXJzZUludChlbC52YWx1ZSwxMCk6cGFyc2VJbnQoZGF0YS5tYXhXcm9uZ3x8MCwxMCk7IHJldHVybiBpc05hTih2KXx8djwwPzA6djt9CmZ1bmN0aW9uIG9wdENoZWNrZWQoaWQsZmFsbGJhY2spe2NvbnN0IGVsPSQoaWQpOyByZXR1cm4gZWw/ZWwuY2hlY2tlZDohIWZhbGxiYWNrO30KZnVuY3Rpb24gcmV2ZWFsKHEsbXNnKXtpZihvcHRDaGVja2VkKCdvcHRTaG93QW5zd2VyJyxkYXRhLnNob3dBbnN3ZXIpKSBtc2crPScgJyt0eHQuYW5zd2VyKyc6ICcrZXNjKHEuY29ycmVjdCk7IHJldHVybiBtc2c7fQpmdW5jdGlvbiBub3JtYWxpemVUZXh0KHZhbHVlKXtyZXR1cm4gU3RyaW5nKHZhbHVlfHwnJykudHJpbSgpLnRvTG93ZXJDYXNlKCkubm9ybWFsaXplKCdORkQnKS5yZXBsYWNlKC9bXHUwMzAwLVx1MDM2Zl0vZywnJykucmVwbGFjZSgvxJEvZywnZCcpLnJlcGxhY2UoL1teYS16MC05XSsvZywnICcpLnRyaW0oKTt9CmZ1bmN0aW9uIHRva2VuaXplKG5vcm1hbGl6ZWQpe2NvbnN0IHN0b3A9bmV3IFNldChbJ2xhJywnbGFtJywndmEnLCd2b2knLCdjdWEnLCdjaG8nLCdtb3QnLCduaHVuZycsJ2NhYycsJ2toaScsJ25ldScsJ3RoaScsJ3RyZW4nLCdkdW9pJywndHJvbmcnLCduZ29haScsJ3RhaScsJ2RvJywnZGF5Jywna2lhJywnbmF5JywnYXknLCdob24nLCdpdCcsJ3JhdCcsJ2NvJywna2hvbmcnLCdkdW9jJywnYmknLCd0dScsJ2RlbicsJ3Z1YScsJ3NlJywndXNlcicsJ21vZGUnLCd0eXBlJywnZHJpdmVyJywna2lldScsJ2xvYWknLCdwaGFuJywnY2F1JywnZGFwJywnYW4nLCdoaWVuJywnZGFpJywnY3UnLCdtb2knLCd0b3QnLCdiZXR0ZXInLCdtb3JlJywnbGVzcycsJ3dpdGgnLCdmcm9tJywnaW50bycsJ3RoYW4nLCd0aGUnLCdhJywnYW4nLCdhbmQnLCdvcicsJ3RvJywnb2YnLCdmb3InLCdpbicsJ29uJ10pOyByZXR1cm4gbm9ybWFsaXplZC5zcGxpdCgvXHMrLykuZmlsdGVyKHQ9PnQubGVuZ3RoPj0yICYmICFzdG9wLmhhcyh0KSkuZmlsdGVyKCh0LGksYSk9PmEuaW5kZXhPZih0KT09PWkpO30KZnVuY3Rpb24gbGV2ZW5zaHRlaW4oYSxiKXtjb25zdCBkcD1BcnJheS5mcm9tKHtsZW5ndGg6YS5sZW5ndGgrMX0sKCk9PkFycmF5KGIubGVuZ3RoKzEpLmZpbGwoMCkpOyBmb3IobGV0IGk9MDtpPD1hLmxlbmd0aDtpKyspZHBbaV1bMF09aTsgZm9yKGxldCBqPTA7ajw9Yi5sZW5ndGg7aisrKWRwWzBdW2pdPWo7IGZvcihsZXQgaT0xO2k8PWEubGVuZ3RoO2krKyl7IGZvcihsZXQgaj0xO2o8PWIubGVuZ3RoO2orKyl7IGNvbnN0IGNvc3Q9YVtpLTFdPT09YltqLTFdPzA6MTsgZHBbaV1bal09TWF0aC5taW4oZHBbaS0xXVtqXSsxLGRwW2ldW2otMV0rMSxkcFtpLTFdW2otMV0rY29zdCk7IH0gfSByZXR1cm4gZHBbYS5sZW5ndGhdW2IubGVuZ3RoXTt9CmZ1bmN0aW9uIHNpbWlsYXJpdHkoYSxiKXsgaWYoIWF8fCFiKSByZXR1cm4gMDsgaWYoYT09PWIpIHJldHVybiAxOyBjb25zdCBkaXN0PWxldmVuc2h0ZWluKGEsYik7IHJldHVybiBNYXRoLm1heCgwLDEtZGlzdC9NYXRoLm1heChhLmxlbmd0aCxiLmxlbmd0aCkpOyB9CmZ1bmN0aW9uIGV2YWx1YXRlRmlsbEJsYW5rKGFuc3dlcixjb3JyZWN0KXsgY29uc3QgbmE9bm9ybWFsaXplVGV4dChhbnN3ZXIpLCBuYz1ub3JtYWxpemVUZXh0KGNvcnJlY3QpOyBjb25zdCByZXN1bHQ9e3Njb3JlUmF0aW86MCxpc0NvcnJlY3Q6ZmFsc2V9OyBpZighbmF8fCFuYykgcmV0dXJuIHJlc3VsdDsgaWYobmE9PT1uYyl7IHJlc3VsdC5zY29yZVJhdGlvPTE7IHJlc3VsdC5pc0NvcnJlY3Q9dHJ1ZTsgcmV0dXJuIHJlc3VsdDsgfSBjb25zdCBjb3JyZWN0VG9rZW5zPXRva2VuaXplKG5jKSwgYW5zd2VyVG9rZW5zPXRva2VuaXplKG5hKTsgY29uc3QgbWF0Y2hlZD1jb3JyZWN0VG9rZW5zLmZpbHRlcih0b2tlbj0+YW5zd2VyVG9rZW5zLnNvbWUoY2FuZGlkYXRlPT5jYW5kaWRhdGU9PT10b2tlbiB8fCBzaW1pbGFyaXR5KGNhbmRpZGF0ZSx0b2tlbik+PTAuODYpKTsgY29uc3Qga2V5d29yZFJhdGlvPWNvcnJlY3RUb2tlbnMubGVuZ3RoP21hdGNoZWQubGVuZ3RoL2NvcnJlY3RUb2tlbnMubGVuZ3RoOjA7IGxldCBsb25nZXN0PTAsY3VycmVudD0wOyBjb3JyZWN0VG9rZW5zLmZvckVhY2godG9rZW49PnsgaWYoYW5zd2VyVG9rZW5zLnNvbWUoY2FuZGlkYXRlPT5jYW5kaWRhdGU9PT10b2tlbiB8fCBzaW1pbGFyaXR5KGNhbmRpZGF0ZSx0b2tlbik+PTAuODYpKXsgY3VycmVudCsrOyBsb25nZXN0PU1hdGgubWF4KGxvbmdlc3QsY3VycmVudCk7fSBlbHNlIGN1cnJlbnQ9MDsgfSk7IGNvbnN0IGNvbnRhaW5tZW50UmF0aW89Y29ycmVjdFRva2Vucy5sZW5ndGg/bG9uZ2VzdC9jb3JyZWN0VG9rZW5zLmxlbmd0aDowOyBjb25zdCBzaW1pbGFyaXR5UmF0aW89c2ltaWxhcml0eShuYSxuYyk7IGNvbnN0IHN0cm9uZ2VzdD1NYXRoLm1heChrZXl3b3JkUmF0aW8sIHNpbWlsYXJpdHlSYXRpbywgY29udGFpbm1lbnRSYXRpbyk7IGNvbnN0IHN0cm9uZ0tleXdvcmRNYXRjaD1rZXl3b3JkUmF0aW8+PTAuODUgJiYgY29udGFpbm1lbnRSYXRpbz49MC42NTsgY29uc3QgbmVhckV4YWN0TWVhbmluZz1zaW1pbGFyaXR5UmF0aW8+PTAuOSB8fCBjb250YWlubWVudFJhdGlvPj0wLjkyOyBpZihrZXl3b3JkUmF0aW8+PTEgfHwgbmVhckV4YWN0TWVhbmluZyB8fCBzdHJvbmdLZXl3b3JkTWF0Y2gpeyByZXN1bHQuc2NvcmVSYXRpbz0xOyByZXN1bHQuaXNDb3JyZWN0PXRydWU7IH0gZWxzZSBpZihrZXl3b3JkUmF0aW8+PTAuNyB8fCBjb250YWlubWVudFJhdGlvPj0wLjc1KXsgcmVzdWx0LnNjb3JlUmF0aW89TWF0aC5tYXgoMC44LHN0cm9uZ2VzdCk7IH0gZWxzZSBpZihzdHJvbmdlc3Q+PTAuNTUpeyByZXN1bHQuc2NvcmVSYXRpbz1zdHJvbmdlc3Q7IH0gcmVzdWx0LnNjb3JlUmF0aW89TWF0aC5yb3VuZChyZXN1bHQuc2NvcmVSYXRpbyoxMDApLzEwMDsgcmV0dXJuIHJlc3VsdDsgfQpmdW5jdGlvbiB1cGRhdGVUaW1lcigpeyBjb25zdCB0aW1lcj0kKCd0aW1lcicpOyBpZighdGltZXIpIHJldHVybjsgY29uc3QgbT1NYXRoLmZsb29yKE1hdGgubWF4KHJlbWFpbmluZ1NlY29uZHMsMCkvNjApLCBzPU1hdGgubWF4KHJlbWFpbmluZ1NlY29uZHMsMCklNjA7IHRpbWVyLnRleHRDb250ZW50PVN0cmluZyhtKS5wYWRTdGFydCgyLCcwJykrJzonK1N0cmluZyhzKS5wYWRTdGFydCgyLCcwJyk7IH0KZnVuY3Rpb24gc3RhcnRUaW1lcigpeyBpZih0aW1lcklkKSBjbGVhckludGVydmFsKHRpbWVySWQpOyB1cGRhdGVUaW1lcigpOyB0aW1lcklkPXNldEludGVydmFsKCgpPT57IGlmKHN1Ym1pdHRlZCl7IGNsZWFySW50ZXJ2YWwodGltZXJJZCk7IHJldHVybjsgfSByZW1haW5pbmdTZWNvbmRzLS07IHVwZGF0ZVRpbWVyKCk7IGlmKHJlbWFpbmluZ1NlY29uZHM8PTApeyBjbGVhckludGVydmFsKHRpbWVySWQpOyBzdWJtaXR0ZWQ9dHJ1ZTsgYWxlcnQodHh0LnRpbWVVcCk7IHNhdmVIaWRkZW4oKTsgJCgnZXhhbUZvcm0nKS5zdWJtaXQoKTsgfSB9LDEwMDApOyB9CmZ1bmN0aW9uIHNldEV4YW1WaXNpYmxlKHYpe1snLmV4YW0tcHJvZ3Jlc3MnLCcjcXVlc3Rpb25Ib3N0JywnLm5hdi1hY3Rpb25zJywnLnF1aWNrLW5hdiddLmZvckVhY2gocz0+e2NvbnN0IGVsPWRvY3VtZW50LnF1ZXJ5U2VsZWN0b3Iocyk7IGlmKGVsKSBlbC5zdHlsZS5kaXNwbGF5PXY/Jyc6J25vbmUnO30pO30KZnVuY3Rpb24gcmVuZGVyTmF2KCl7aWYoIXN0YXJ0ZWQpcmV0dXJuOyBjb25zdCBnPSQoJ25hdkdyaWQnKTsgaWYoIWcpcmV0dXJuOyBnLmlubmVySFRNTD0nJzsgcXMuZm9yRWFjaCgocSxpKT0+e2NvbnN0IGI9ZG9jdW1lbnQuY3JlYXRlRWxlbWVudCgnYnV0dG9uJyk7IGIudHlwZT0nYnV0dG9uJzsgYi50ZXh0Q29udGVudD1pKzE7IGIuY2xhc3NOYW1lPShpPT09aWR4PydhY3RpdmUgJzonJykrKGFuc3dlcnNbcS5pZF0/J2Fuc3dlcmVkICc6JycpKyhsb2NrZWRbcS5pZF0/KGVhcm5lZFtxLmlkXT4wPydwYXNzZWQnOidmYWlsZWQnKTonJyk7IGIub25jbGljaz0oKT0+e2lkeD1pO3JlbmRlcigpO307IGcuYXBwZW5kQ2hpbGQoYik7fSk7IGNvbnN0IHNjb3JlPSQoJ3ByYWN0aWNlU2NvcmUnKTsgaWYoc2NvcmUpIHNjb3JlLnRleHRDb250ZW50PWVhcm5lZERpc3BsYXkoKTt9CmZ1bmN0aW9uIHJlbmRlcigpe2lmKCFzdGFydGVkKXJldHVybjsgaWYoIXFzLmxlbmd0aCl7JCgncXVlc3Rpb25Ib3N0JykuaW5uZXJIVE1MPSc8cD5DaMawYSBjw7MgY8OidSBo4buPaS48L3A+JztyZXR1cm47fSBjb25zdCBxPXFzW2lkeF07ICQoJ3F1ZXN0aW9uQ291bnRlcicpLnRleHRDb250ZW50PXR4dC5xKycgJysoaWR4KzEpKycvJytxcy5sZW5ndGg7ICQoJ2Fuc3dlcmVkQ291bnRlcicpLnRleHRDb250ZW50PWFuc3dlcmVkQ291bnQoKSsnLycrcXMubGVuZ3RoKycgJyt0eHQuYW5zd2VyZWQ7IGxldCBoPSc8ZGl2IGNsYXNzPSJxLWhlYWQiPjxzcGFuIGNsYXNzPSJwaWxsIj4nK3R4dC5xKycgJysoaWR4KzEpKyc8L3NwYW4+PHNwYW4gY2xhc3M9Im11dGVkIj4nK2VzYyhxLnR5cGVUZXh0KSsnPC9zcGFuPjwvZGl2PjxoMj4nK2VzYyhxLmNvbnRlbnQpKyc8L2gyPic7IGNvbnN0IGlzTG9ja2VkPSEhbG9ja2VkW3EuaWRdOyBpZihxLnR5cGU9PT0nRmlsbEJsYW5rJyl7aCs9JzxsYWJlbD4nK3R4dC5lbnRlckFuc3dlcisnPC9sYWJlbD48aW5wdXQgaWQ9ImZpbGxBbnN3ZXIiICcrKGlzTG9ja2VkPydkaXNhYmxlZCc6JycpKycgdmFsdWU9IicrZXNjKGFuc3dlcnNbcS5pZF18fCcnKSsnIiBwbGFjZWhvbGRlcj0iTmjhuq1wIGPDonUgdHLhuqMgbOG7nWkuLi4iIC8+PGJ1dHRvbiBpZD0iYnRuQ2hlY2siIGNsYXNzPSJidG4gcHJpbWFyeSIgdHlwZT0iYnV0dG9uIiAnKyhpc0xvY2tlZD8nZGlzYWJsZWQnOicnKSsnPicrdHh0LmNoZWNrKyc8L2J1dHRvbj4nO30gZWxzZSB7aCs9JzxkaXYgY2xhc3M9Im9wdGlvbnMiPic7IFsnQScsJ0InLCdDJywnRCddLmZvckVhY2goaz0+e2lmKHEub3B0aW9uc1trXSkgaCs9JzxsYWJlbCBjbGFzcz0ib3B0aW9uICcrKGlzTG9ja2VkPydsb2NrZWQnOicnKSsnIj48aW5wdXQgdHlwZT0icmFkaW8iIG5hbWU9ImNob2ljZSIgdmFsdWU9IicraysnIiAnKyhhbnN3ZXJzW3EuaWRdPT09az8nY2hlY2tlZCc6JycpKycgJysoaXNMb2NrZWQ/J2Rpc2FibGVkJzonJykrJyAvPiA8c3BhbiBjbGFzcz0ib3B0aW9uLWtleSI+JytrKycuPC9zcGFuPjxzcGFuPicrZXNjKHEub3B0aW9uc1trXSkrJzwvc3Bhbj48L2xhYmVsPic7fSk7IGgrPSc8L2Rpdj4nO30KaWYoZGF0YS5pc1ByYWN0aWNlKXtjb25zdCBmZWVkYmFjaz1wcmFjdGljZUZlZWRiYWNrW3EuaWRdOyBoKz0nPGRpdiBpZD0icHJhY3RpY2VNc2ciIGNsYXNzPSJwcmFjdGljZS1tc2cgJysoZmVlZGJhY2s/KGZlZWRiYWNrLm9rPydvayc6J2JhZCcpOicnKSsnIj4nKyhmZWVkYmFjaz9mZWVkYmFjay5odG1sKyc8YnI+JzonJykrdHh0LnBvaW50Kyc6ICcrZWFybmVkRGlzcGxheSgpKyc8L2Rpdj4nO30gJCgncXVlc3Rpb25Ib3N0JykuaW5uZXJIVE1MPWg7IGJpbmRRdWVzdGlvbihxKTsgcmVuZGVyTmF2KCk7fQpmdW5jdGlvbiBiaW5kUXVlc3Rpb24ocSl7aWYocS50eXBlPT09J0ZpbGxCbGFuaycpe2NvbnN0IGlucD0kKCdmaWxsQW5zd2VyJyk7IGlmKGlucCkgaW5wLm9uaW5wdXQ9KCk9PntpZighc3RhcnRlZClyZXR1cm47IGFuc3dlcnNbcS5pZF09aW5wLnZhbHVlOyBzYXZlSGlkZGVuKCk7IHJlbmRlck5hdigpO307IGNvbnN0IGJ0bj0kKCdidG5DaGVjaycpOyBpZihidG4pIGJ0bi5vbmNsaWNrPSgpPT5jaGVja0ZpbGwocSk7fSBlbHNlIGRvY3VtZW50LnF1ZXJ5U2VsZWN0b3JBbGwoJ2lucHV0W25hbWU9Y2hvaWNlXScpLmZvckVhY2gocj0+ci5vbmNoYW5nZT0oKT0+e2lmKCFzdGFydGVkKXJldHVybjsgYW5zd2Vyc1txLmlkXT1yLnZhbHVlOyBpZihkYXRhLmlzUHJhY3RpY2UpIGNoZWNrQ2hvaWNlKHEsci52YWx1ZSk7IHNhdmVIaWRkZW4oKTsgcmVuZGVyKCk7fSk7fQpmdW5jdGlvbiBub3JtKHMpe3JldHVybiBTdHJpbmcoc3x8JycpLnRyaW0oKS50b0xvd2VyQ2FzZSgpO30KZnVuY3Rpb24gaXNDb3JyZWN0KHEsYSl7cmV0dXJuIHEudHlwZT09PSdGaWxsQmxhbmsnP25vcm0oYSk9PT1ub3JtKHEuY29ycmVjdCk6U3RyaW5nKGF8fCcnKS50b1VwcGVyQ2FzZSgpPT09U3RyaW5nKHEuY29ycmVjdHx8JycpLnRvVXBwZXJDYXNlKCk7fQpmdW5jdGlvbiBzZXRNc2cocWlkLGh0bWwsb2spe3ByYWN0aWNlRmVlZGJhY2tbcWlkXT17aHRtbDpodG1sLG9rOiEhb2t9OyBjb25zdCBtPSQoJ3ByYWN0aWNlTXNnJyk7IGlmKCFtKXJldHVybjsgbS5jbGFzc05hbWU9J3ByYWN0aWNlLW1zZyAnKyhvaz8nb2snOidiYWQnKTsgbS5pbm5lckhUTUw9aHRtbCsnPGJyPicrdHh0LnBvaW50Kyc6ICcrZWFybmVkRGlzcGxheSgpO30KZnVuY3Rpb24gbG9ja1F1ZXN0aW9uKHEscG9pbnRzKXtlYXJuZWRbcS5pZF09TnVtYmVyKHBvaW50cyl8fDA7IGxvY2tlZFtxLmlkXT10cnVlO30KZnVuY3Rpb24gY2hlY2tDaG9pY2UocSxhKXtpZihsb2NrZWRbcS5pZF0pcmV0dXJuOyBjb25zdCBvaz1pc0NvcnJlY3QocSxhKTsgaWYob2spe2NvbnN0IHJldHJpZWQ9KHdyb25nW3EuaWRdfHwwKT4wOyBsb2NrUXVlc3Rpb24ocSxxLnBvaW50cyk7IGxldCBtc2c9cmV0cmllZD90eHQuY29ycmVjdFJldHJ5OihvcHRDaGVja2VkKCdvcHRTaG93UG9pbnRHYWluJyxkYXRhLnNob3dQb2ludEdhaW4pP3R4dC5jb3JyZWN0KychICsnK2ZtdCh0b0Rpc3BsYXkocS5wb2ludHMpKSsnIMSRaeG7g20uJzp0eHQuY29ycmVjdCsnLicpOyBzZXRNc2cocS5pZCxyZXZlYWwocSxtc2cpLHRydWUpOyBpZihvcHRDaGVja2VkKCdvcHRBdXRvTmV4dCcsZGF0YS5hdXRvTmV4dCkpIHNldFRpbWVvdXQoKCk9PntpZihpZHg8cXMubGVuZ3RoLTEpe2lkeCsrO3JlbmRlcigpO319LDQ1MCk7fSBlbHNlIHt3cm9uZ1txLmlkXT0od3JvbmdbcS5pZF18fDApKzE7IGVhcm5lZFtxLmlkXT0wOyBjb25zdCByZW1haW5pbmc9Y3VycmVudE1heFdyb25nKCktd3JvbmdbcS5pZF07IGlmKHJlbWFpbmluZz4wKXtzZXRNc2cocS5pZCx0eHQud3JvbmdSZXRyeS5yZXBsYWNlKCd7bn0nLHJlbWFpbmluZyksZmFsc2UpO30gZWxzZSB7bG9ja2VkW3EuaWRdPXRydWU7IHNldE1zZyhxLmlkLHJldmVhbChxLHR4dC53cm9uZ0xvY2tlZCksZmFsc2UpOyByZW5kZXIoKTt9fSBzYXZlSGlkZGVuKCk7IHJlbmRlck5hdigpO30KZnVuY3Rpb24gY2hlY2tGaWxsKHEpe2lmKGxvY2tlZFtxLmlkXSlyZXR1cm47IGNvbnN0IGlucHV0PSQoJ2ZpbGxBbnN3ZXInKTsgY29uc3QgYT1pbnB1dD9pbnB1dC52YWx1ZTonJzsgaWYoIVN0cmluZyhhKS50cmltKCkpe3NldE1zZyhxLmlkLHR4dC5maWxsRW1wdHksZmFsc2UpO3JldHVybjt9IGFuc3dlcnNbcS5pZF09YTsgY29uc3QgZXZhbHVhdGlvbj1ldmFsdWF0ZUZpbGxCbGFuayhhLHEuY29ycmVjdCk7IGlmKGV2YWx1YXRpb24uc2NvcmVSYXRpbz4wKXtjb25zdCBnYWluZWQ9KE51bWJlcihxLnBvaW50cyl8fDApKmV2YWx1YXRpb24uc2NvcmVSYXRpbzsgbG9ja1F1ZXN0aW9uKHEsZ2FpbmVkKTsgaWYoZXZhbHVhdGlvbi5pc0NvcnJlY3Qpe2xldCBtc2c9b3B0Q2hlY2tlZCgnb3B0U2hvd1BvaW50R2FpbicsZGF0YS5zaG93UG9pbnRHYWluKT90eHQuY29ycmVjdCsnISArJytmbXQodG9EaXNwbGF5KGdhaW5lZCkpKycgxJFp4buDbS4nOnR4dC5jb3JyZWN0KycuJzsgc2V0TXNnKHEuaWQscmV2ZWFsKHEsbXNnKSx0cnVlKTsgc2F2ZUhpZGRlbigpOyByZW5kZXJOYXYoKTsgaWYob3B0Q2hlY2tlZCgnb3B0QXV0b05leHQnLGRhdGEuYXV0b05leHQpKSBzZXRUaW1lb3V0KCgpPT57aWYoaWR4PHFzLmxlbmd0aC0xKXtpZHgrKztyZW5kZXIoKTt9fSw0NTApOyByZXR1cm47fSBzZXRNc2cocS5pZCxyZXZlYWwocSx0eHQubmVhci5yZXBsYWNlKCd7Z2Fpbn0nLGZtdCh0b0Rpc3BsYXkoZ2FpbmVkKSkpLnJlcGxhY2UoJ3t0b3RhbH0nLGZtdCh0b0Rpc3BsYXkocS5wb2ludHMpKSkpLHRydWUpOyByZW5kZXIoKTsgc2F2ZUhpZGRlbigpOyByZW5kZXJOYXYoKTsgcmV0dXJuO30gd3JvbmdbcS5pZF09KHdyb25nW3EuaWRdfHwwKSsxOyBlYXJuZWRbcS5pZF09MDsgY29uc3QgcmVtYWluaW5nPWN1cnJlbnRNYXhXcm9uZygpLXdyb25nW3EuaWRdOyBpZihyZW1haW5pbmc+MCl7c2V0TXNnKHEuaWQsdHh0LmZpbGxSZXRyeS5yZXBsYWNlKCd7bn0nLHJlbWFpbmluZyksZmFsc2UpO30gZWxzZSB7bG9ja2VkW3EuaWRdPXRydWU7IHNldE1zZyhxLmlkLHJldmVhbChxLHR4dC5maWxsTG9ja2VkKSxmYWxzZSk7IHJlbmRlcigpO30gc2F2ZUhpZGRlbigpOyByZW5kZXJOYXYoKTt9CmZ1bmN0aW9uIHNhdmVIaWRkZW4oKXtjb25zdCBhPSQoJ2Fuc3dlcnNKc29uJyksIGU9JCgnZWFybmVkSnNvbicpOyBpZihhKWEudmFsdWU9SlNPTi5zdHJpbmdpZnkoYW5zd2Vycyk7IGlmKGUpZS52YWx1ZT1KU09OLnN0cmluZ2lmeShlYXJuZWQpO30KZnVuY3Rpb24gc3RhcnQoKXtpZihkYXRhLnJlcXVpcmVDbGFzcyAmJiAhZG9jdW1lbnQucXVlcnlTZWxlY3RvcignW25hbWU9c3R1ZGVudENsYXNzXScpLnZhbHVlLnRyaW0oKSl7YWxlcnQodHh0Lm5lZWRDbGFzcyk7cmV0dXJuO30gaWYoISQoJ2NvbmZpcm1TdGFydCcpLmNoZWNrZWQpe2FsZXJ0KHR4dC5uZWVkQ29uZmlybSk7cmV0dXJuO30gZGF0YS5tYXhXcm9uZz1jdXJyZW50TWF4V3JvbmcoKTsgZGF0YS5zaG93QW5zd2VyPW9wdENoZWNrZWQoJ29wdFNob3dBbnN3ZXInLGRhdGEuc2hvd0Fuc3dlcik7IGRhdGEuc2hvd1BvaW50R2Fpbj1vcHRDaGVja2VkKCdvcHRTaG93UG9pbnRHYWluJyxkYXRhLnNob3dQb2ludEdhaW4pOyBkYXRhLmF1dG9OZXh0PW9wdENoZWNrZWQoJ29wdEF1dG9OZXh0JyxkYXRhLmF1dG9OZXh0KTsgc3RhcnRlZD10cnVlOyAkKCdzdGFydFBhbmVsJykuc3R5bGUuZGlzcGxheT0nbm9uZSc7IHNldEV4YW1WaXNpYmxlKHRydWUpOyBzdGFydFRpbWVyKCk7IHJlbmRlcigpO30KJCgnYnRuU3RhcnQnKS5vbmNsaWNrPXN0YXJ0OyAkKCdidG5QcmV2Jykub25jbGljaz0oKT0+e2lmKGlkeD4wKXtpZHgtLTtyZW5kZXIoKTt9fTsgJCgnYnRuTmV4dCcpLm9uY2xpY2s9KCk9PntpZihpZHg8cXMubGVuZ3RoLTEpe2lkeCsrO3JlbmRlcigpO319OyAkKCdidG5DbGVhcicpLm9uY2xpY2s9KCk9PntpZihxc1tpZHhdJiYhbG9ja2VkW3FzW2lkeF0uaWRdKXtkZWxldGUgYW5zd2Vyc1txc1tpZHhdLmlkXTtkZWxldGUgZWFybmVkW3FzW2lkeF0uaWRdO2RlbGV0ZSB3cm9uZ1txc1tpZHhdLmlkXTtkZWxldGUgcHJhY3RpY2VGZWVkYmFja1txc1tpZHhdLmlkXTtzYXZlSGlkZGVuKCk7cmVuZGVyKCk7fX07ICQoJ2J0blN1Ym1pdCcpLm9uY2xpY2s9KCk9PntzYXZlSGlkZGVuKCk7IGlmKGNvbmZpcm0odHh0LnN1Ym1pdCkpeyBzdWJtaXR0ZWQ9dHJ1ZTsgaWYodGltZXJJZCkgY2xlYXJJbnRlcnZhbCh0aW1lcklkKTsgJCgnZXhhbUZvcm0nKS5zdWJtaXQoKTsgfX07CndpbmRvdy5hZGRFdmVudExpc3RlbmVyKCdiZWZvcmV1bmxvYWQnLGU9PnsgaWYoc3RhcnRlZCAmJiAhc3VibWl0dGVkKXsgZS5wcmV2ZW50RGVmYXVsdCgpOyBlLnJldHVyblZhbHVlPXR4dC5sZWF2ZTsgcmV0dXJuIHR4dC5sZWF2ZTsgfX0pOwp1cGRhdGVUaW1lcigpOwpzZXRFeGFtVmlzaWJsZShmYWxzZSk7Cjwvc2NyaXB0Pg=="));
        }

        private string SubmitExam(string roomCode, Dictionary<string, string> form)
        {
            var exam = FindExam(roomCode);
            if (exam == null) return ErrorPage(V("Khong tim thay bai thi"), "");
            int userId = ToInt(Get(form, "userId"));
            int examId = ToInt(Get(form, "examId"));
            var questions = LoadQuestionsByIds(examId, Get(form, "questionIds"));
            var answers = ParseJsonMap(Get(form, "answersJson"));
            var earnedMap = ParseJsonMap(Get(form, "earnedJson"));
            bool isPractice = Eq(S(exam, "ExamMode"), "Practice");
            decimal total = questions.Sum(q => D(q, "Points", 1));
            if (total <= 0) total = questions.Count;
            decimal earned = 0;
            int correctCount = 0;
            int answeredCount = 0;
            var reviewRows = new List<string>();
            int questionNumber = 1;
            foreach (var q in questions)
            {
                string qid = I(q, "QuestionID", 0).ToString(CultureInfo.InvariantCulture);
                string answer = answers.ContainsKey(qid) ? answers[qid] : "";
                bool hasAnswer = !string.IsNullOrWhiteSpace(answer);
                bool correct = IsCorrect(q, answer);
                decimal awarded = isPractice && earnedMap.ContainsKey(qid) ? ToDec(earnedMap[qid]) : CalculateAwardedPoints(q, answer);
                decimal points = D(q, "Points", 1);

                if (hasAnswer) answeredCount++;
                if (correct) correctCount++;
                earned += awarded;
                reviewRows.Add(BuildResultReviewRow(questionNumber, q, answer, correct, awarded, points));
                questionNumber++;
            }
            decimal scale = D(exam, "ScoreScale", 10);
            decimal score = total <= 0 ? 0 : Math.Round(earned / total * scale, 2);
            string fullName = Get(form, "studentFullName");
            string className = Get(form, "studentClass");

            var db = new Database();
            if (userId > 0) db.ExecuteNonQuery("UPDATE Users SET FullName=N'" + Sql(fullName) + "', ClassName=N'" + Sql(className) + "' WHERE UserID=" + userId);
            db.ExecuteNonQuery("INSERT INTO Results (UserID, ExamID, Score, SubmitDate, StudentFullName, StudentClass) VALUES (" + userId + "," + examId + "," + score.ToString(CultureInfo.InvariantCulture) + ",GETDATE(),N'" + Sql(fullName) + "',N'" + Sql(className) + "')");
            var ridTable = db.ExecuteQuery("SELECT IDENT_CURRENT('Results') AS ResultID");
            int resultId = ridTable.Rows.Count > 0 ? Convert.ToInt32(ridTable.Rows[0]["ResultID"]) : 0;
            int order = 1;
            foreach (var q in questions)
            {
                string qid = I(q, "QuestionID", 0).ToString(CultureInfo.InvariantCulture);
                string answer = answers.ContainsKey(qid) ? answers[qid] : "";
                bool correct = IsCorrect(q, answer);
                decimal points = D(q, "Points", 1);
                decimal awarded = isPractice && earnedMap.ContainsKey(qid) ? ToDec(earnedMap[qid]) : CalculateAwardedPoints(q, answer);
                string sql = "INSERT INTO ExamAttemptAnswers (ResultID,UserID,ExamID,QuestionID,QuestionOrder,QuestionContent,OptionA,OptionB,OptionC,OptionD,CorrectAnswer,UserAnswer,IsCorrect,QuestionPoints,AwardedPoints) VALUES (" +
                    resultId + "," + userId + "," + examId + "," + qid + "," + order + ",N'" + Sql(S(q, "QuestionContent")) + "',N'" + Sql(S(q, "OptionA")) + "',N'" + Sql(S(q, "OptionB")) + "',N'" + Sql(S(q, "OptionC")) + "',N'" + Sql(S(q, "OptionD")) + "',N'" + Sql(S(q, "CorrectAnswer")) + "',N'" + Sql(answer) + "'," + (correct ? "1" : "0") + "," + points.ToString(CultureInfo.InvariantCulture) + "," + awarded.ToString(CultureInfo.InvariantCulture) + ")";
                db.ExecuteNonQuery(sql);
                order++;
            }
            return BuildResultPage(S(exam, "ExamName"), score, scale, earned, total, correctCount, answeredCount, questions.Count, reviewRows);
        }

        private string BuildResultPage(string title, decimal score, decimal scale, decimal earned, decimal total, int correctCount, int answeredCount, int totalQuestions, List<string> reviewRows)
        {
            string headline = BuildResultHeadline(score, scale);
            string hint = BuildResultHint(score, scale, correctCount, answeredCount, totalQuestions);
            string reviewHtml = reviewRows != null && reviewRows.Count > 0
                ? string.Join("", reviewRows)
                : "<tr><td colspan='5'>" + T(V("Ch\u01b0a c\u00f3 d\u1eef li\u1ec7u chi ti\u1ebft b\u00e0i l\u00e0m.")) + "</td></tr>";

            return Layout(title, @"
<main class='result-wrap'>
  <section class='result-card-wide'>
    <span class='pill success'>" + T(V("Da nop bai")) + @"</span>
    <h1>" + T(V("Ket qua bai thi")) + @"</h1>
    <div class='summary-grid'>
      <div class='summary-box blue'><span>" + T(V("\u0110i\u1ec3m ch\u00ednh th\u1ee9c")) + @"</span><strong>" + score.ToString("0.##", CultureInfo.InvariantCulture) + @"/" + scale.ToString("0.##", CultureInfo.InvariantCulture) + @"</strong></div>
      <div class='summary-box green'><span>" + T(V("S\u1ed1 c\u00e2u \u0111\u00fang")) + @"</span><strong>" + correctCount + @"/" + totalQuestions + @"</strong></div>
      <div class='summary-box orange'><span>" + T(V("\u0110i\u1ec3m theo t\u1eebng c\u00e2u")) + @"</span><strong>" + earned.ToString("0.##", CultureInfo.InvariantCulture) + @"/" + total.ToString("0.##", CultureInfo.InvariantCulture) + @"</strong></div>
      <div class='summary-box purple'><span>" + T(V("C\u00e2u \u0111\u00e3 tr\u1ea3 l\u1eddi")) + @"</span><strong>" + answeredCount + @"/" + totalQuestions + @"</strong></div>
    </div>
    <section class='result-note'>
      <h2>" + Enc(headline) + @"</h2>
      <p>" + Enc(hint) + @"</p>
    </section>
    <section class='result-detail'>
      <div class='detail-head'>
        <h2>" + T(V("Chi ti\u1ebft b\u00e0i l\u00e0m")) + @"</h2>
        <p class='muted'>" + T(V("M\u1ed7i d\u00f2ng cho bi\u1ebft n\u1ed9i dung c\u00e2u h\u1ecfi, \u0111\u00e1p \u00e1n b\u1ea1n \u0111\u00e3 ch\u1ecdn, \u0111\u00e1p \u00e1n \u0111\u00fang v\u00e0 k\u1ebft qu\u1ea3.")) + @"</p>
      </div>
      <div class='review-table-wrap'>
        <table class='review-table'>
          <thead>
            <tr>
              <th>" + T(V("Cau")) + @"</th>
              <th>" + T(V("N\u1ed9i dung")) + @"</th>
              <th>" + T(V("B\u1ea1n tr\u1ea3 l\u1eddi")) + @"</th>
              <th>" + T(V("\u0110\u00e1p \u00e1n \u0111\u00fang")) + @"</th>
              <th>" + T(V("K\u1ebft qu\u1ea3")) + @"</th>
            </tr>
          </thead>
          <tbody>" + reviewHtml + @"</tbody>
        </table>
      </div>
    </section>
  </section>
</main>");
        }

        private string BuildResultReviewRow(int number, DataRow q, string answer, bool correct, decimal awarded, decimal points)
        {
            string resultText = correct ? V("\u0110\u00fang") : (awarded > 0 ? V("G\u1ea7n \u0111\u00fang") : V("Sai"));
            string rowClass = correct ? "ok" : (awarded > 0 ? "partial" : "bad");
            string userAnswer = string.IsNullOrWhiteSpace(answer) ? V("(b\u1ecf tr\u1ed1ng)") : answer;
            return "<tr class='" + rowClass + "'>" +
                "<td>" + number + "</td>" +
                "<td>" + Enc(S(q, "QuestionContent")) + "</td>" +
                "<td>" + Enc(userAnswer) + "</td>" +
                "<td>" + Enc(S(q, "CorrectAnswer")) + "</td>" +
                "<td><span class='result-badge " + rowClass + "'>" + T(resultText) + "</span><div class='cell-sub'>" + awarded.ToString("0.##", CultureInfo.InvariantCulture) + "/" + points.ToString("0.##", CultureInfo.InvariantCulture) + " " + T(V("\u0110i\u1ec3m")) + "</div></td>" +
                "</tr>";
        }

        private string BuildResultHeadline(decimal score, decimal scale)
        {
            decimal normalized = scale <= 0 ? 0 : score / scale * 10M;
            if (normalized >= 8M) return V("B\u1ea1n \u0111\u00e3 ho\u00e0n th\u00e0nh b\u00e0i thi r\u1ea5t t\u1ed1t.");
            if (normalized >= 5M) return V("B\u1ea1n \u0111\u00e3 ho\u00e0n th\u00e0nh b\u00e0i thi \u0111\u1ea1t y\u00eau c\u1ea7u.");
            return V("B\u00e0i l\u00e0m \u0111\u00e3 \u0111\u01b0\u1ee3c l\u01b0u, b\u1ea1n n\u00ean xem l\u1ea1i c\u00e1c c\u00e2u ch\u01b0a \u0111\u00fang.");
        }

        private string BuildResultHint(decimal score, decimal scale, int correctCount, int answeredCount, int totalQuestions)
        {
            decimal normalized = scale <= 0 ? 0 : score / scale * 10M;
            int unanswered = Math.Max(0, totalQuestions - answeredCount);
            string accuracy = totalQuestions > 0
                ? V("T\u1ef7 l\u1ec7 \u0111\u00fang hi\u1ec7n t\u1ea1i l\u00e0 ") + Math.Round(correctCount * 100M / totalQuestions, 0).ToString("0", CultureInfo.InvariantCulture) + "%."
                : V("B\u00e0i l\u00e0m hi\u1ec7n ch\u01b0a c\u00f3 \u0111\u1ee7 d\u1eef li\u1ec7u \u0111\u1ec3 t\u00ednh t\u1ef7 l\u1ec7 \u0111\u00fang.");
            string unansweredHint = unanswered > 0
                ? " " + V("B\u1ea1n c\u00f2n b\u1ecf tr\u1ed1ng ") + unanswered + " " + V("c\u00e2u.")
                : " " + V("B\u1ea1n \u0111\u00e3 tr\u1ea3 l\u1eddi to\u00e0n b\u1ed9 c\u00e2u h\u1ecfi.");

            if (normalized >= 8M) return accuracy + unansweredHint + " " + V("B\u1ea1n c\u00f3 th\u1ec3 xem l\u1ea1i chi ti\u1ebft b\u00ean d\u01b0\u1edbi \u0111\u1ec3 ghi nh\u1edb c\u00e1c c\u00e2u l\u00e0m t\u1ed1t.");
            if (normalized >= 5M) return accuracy + unansweredHint + " " + V("H\u00e3y xem l\u1ea1i m\u1ed9t v\u00e0i c\u00e2u sai \u0111\u1ec3 c\u1ee7ng c\u1ed1 ph\u1ea7n ki\u1ebfn th\u1ee9c c\u00f2n thi\u1ebfu.");
            return accuracy + unansweredHint + " " + V("N\u00ean xem k\u1ef9 ph\u1ea7n \u0111\u00e1p \u00e1n \u0111\u00fang b\u00ean d\u01b0\u1edbi \u0111\u1ec3 bi\u1ebft n\u1ed9i dung c\u1ea7n \u00f4n t\u1eadp th\u00eam.");
        }

        private DataRow FindExam(string roomCode)
        {
            var db = new Database();
            string room = Sql(roomCode);
            var dt = db.ExecuteQuery("SELECT TOP 1 * FROM Exams WHERE RoomCode=N'" + room + "' OR ExamLink LIKE N'%" + room + "%' ORDER BY ExamID DESC");
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private DataRow LoginStudent(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            var db = new Database();
            var dt = db.ExecuteQuery("SELECT TOP 1 * FROM Users WHERE Username=N'" + Sql(username) + "' AND Password=N'" + Sql(password) + "'");
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private List<DataRow> LoadQuestions(DataRow exam)
        {
            int examId = I(exam, "ExamID", 0);
            var db = new Database();
            string sql = @"SELECT q.QuestionID,q.QuestionContent,q.OptionA,q.OptionB,q.OptionC,q.OptionD,q.CorrectAnswer,
CASE WHEN COL_LENGTH('Questions','QuestionType') IS NULL THEN N'MultipleChoice' ELSE ISNULL(q.QuestionType,N'MultipleChoice') END AS QuestionType,
ISNULL(eq.Points, ISNULL(q.Points,1)) AS Points
FROM ExamQuestions eq INNER JOIN Questions q ON eq.QuestionID=q.QuestionID WHERE eq.ExamID=" + examId + " ORDER BY eq.QuestionOrder, q.QuestionID";
            var rows = db.ExecuteQuery(sql).Rows.Cast<DataRow>().ToList();
            if (B(exam, "IsRandomQuestion", false))
            {
                int take = I(exam, "RandomQuestionCount", rows.Count);
                rows = rows.OrderBy(_ => Guid.NewGuid()).Take(Math.Max(1, Math.Min(take, rows.Count))).ToList();
            }
            return rows;
        }

        private List<DataRow> LoadQuestionsByIds(int examId, string ids)
        {
            var clean = string.Join(",", (ids ?? "").Split(',').Select(x => ToInt(x)).Where(x => x > 0));
            if (string.IsNullOrEmpty(clean)) return new List<DataRow>();
            var db = new Database();
            string sql = @"SELECT 
q.QuestionID,q.QuestionContent,q.OptionA,q.OptionB,q.OptionC,q.OptionD,q.CorrectAnswer,
  CASE WHEN COL_LENGTH('Questions','QuestionType') IS NULL THEN N'MultipleChoice' ELSE ISNULL(q.QuestionType,N'MultipleChoice') END AS QuestionType,
  ISNULL(eq.Points, ISNULL(q.Points,1)) AS Points
  FROM ExamQuestions eq INNER JOIN Questions q ON eq.QuestionID=q.QuestionID
  WHERE eq.ExamID=" + examId + " AND q.QuestionID IN (" + clean + ")";
            var map = db.ExecuteQuery(sql).Rows.Cast<DataRow>().ToDictionary(r => I(r, "QuestionID", 0));
            return clean.Split(',').Select(x => ToInt(x)).Where(map.ContainsKey).Select(x => map[x]).ToList();
        }

        private string CanOpenExam(DataRow exam)
        {
            DateTime now = DateTime.Now;
            if (Has(exam, "StartTime") && exam["StartTime"] != DBNull.Value && Convert.ToDateTime(exam["StartTime"]) > now) return V("Bai thi chua den gio mo.");
            if (Has(exam, "EndTime") && exam["EndTime"] != DBNull.Value && Convert.ToDateTime(exam["EndTime"]) < now) return V("Bai thi da het han.");
            return "";
        }
        private string BuildQuestionsJson(List<DataRow> questions)
        {
            var items = new List<string>();
            foreach (var q in questions)
            {
                string type = NormalizeQuestionType(S(q, "QuestionType"));
                items.Add("{id:" + I(q, "QuestionID", 0) + ",content:" + Js(S(q, "QuestionContent")) + ",type:" + Js(type) + ",typeText:" + Js(type == "FillBlank" ? V("Tu luan ngan / dien dap an") : V("Trac nghiem mot dap an")) + ",correct:" + Js(S(q, "CorrectAnswer")) + ",points:" + D(q, "Points", 1).ToString(CultureInfo.InvariantCulture) + ",options:{A:" + Js(S(q, "OptionA")) + ",B:" + Js(S(q, "OptionB")) + ",C:" + Js(S(q, "OptionC")) + ",D:" + Js(S(q, "OptionD")) + "}}");
            }
            return "[" + string.Join(",", items) + "]";
        }

        private string Layout(string title, string body)
        {
            return "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>" + Enc(title) + "</title>" + Styles() + "</head><body>" + body + "</body></html>";
        }

        private string Styles()
        {
            return @"<style>
*{box-sizing:border-box}body{margin:0;font-family:'Segoe UI',Tahoma,Arial,sans-serif;background:linear-gradient(180deg,#eef5ff,#f8fbff);color:#07142f;font-size:16px}.auth-wrap{min-height:100vh;display:flex;align-items:flex-start;justify-content:center;padding:6vh 16px}.auth-card{width:min(520px,100%);background:#fff;border:1px solid #d6e2f1;border-radius:22px;padding:28px;box-shadow:0 28px 70px rgba(29,78,137,.12)}.auth-card--login{background:linear-gradient(180deg,#ffffff,#f8fbff)}h1{font-size:32px;margin:18px 0 20px}h2{font-size:18px;margin:0 0 18px;font-weight:600}h3{font-size:14px;margin:0 0 16px;color:#475b78}.muted{color:#65748b}.pill{display:inline-flex;background:#e6f1ff;color:#1957d2;border-radius:999px;padding:8px 14px;font-weight:600}.pill-soft{background:linear-gradient(135deg,#dbeafe,#eff6ff);color:#1d4ed8}.msg{padding:12px 14px;border-radius:14px;margin:12px 0}.error{background:#fff1f2;color:#d81e3c}label{display:block;font-weight:600;margin:16px 0 8px}input{width:100%;border:1px solid #cbd8ea;border-radius:13px;padding:12px 14px;font:inherit;background:#fff}.btn{border:0;border-radius:13px;padding:12px 20px;font-weight:600;cursor:pointer}.primary{background:#2f67e8;color:#fff}.ghost{background:#edf3ff;color:#17417f}.exam-shell{max-width:1280px;margin:0 auto;padding:36px 18px;display:grid;grid-template-columns:minmax(0,1fr) 320px;gap:20px}.exam-main{min-width:0}.card{background:#fff;border:1px solid #d6e2f1;border-radius:20px;padding:22px;box-shadow:0 18px 54px rgba(16,58,112,.08)}.exam-hero{display:flex;justify-content:space-between;gap:18px;align-items:center;margin-bottom:18px}.timer{background:linear-gradient(135deg,#edf5ff,#dbeafe);border-radius:20px;padding:18px 28px;text-align:center;min-width:220px;box-shadow:inset 0 1px 0 rgba(255,255,255,.65)}.timer span{display:block;text-transform:uppercase;letter-spacing:.08em;color:#2f67e8;font-size:12px}.timer strong{font-size:34px;color:#17358f}.timer small{display:block;margin-top:8px;color:#45607f}.timer.timer-unlimited strong{font-size:30px;line-height:1.12;letter-spacing:-.02em}.grid2{display:grid;grid-template-columns:1fr 1fr;gap:16px}.start-panel{background:linear-gradient(180deg,#ffffff,#f8fbff);border-color:#cfe0f5}.start-hero{display:flex;justify-content:space-between;gap:18px;align-items:flex-start;margin-bottom:18px}.start-highlight{max-width:260px;padding:14px 16px;border-radius:16px;background:linear-gradient(135deg,#1d4ed8,#60a5fa);color:#fff;font-weight:600;line-height:1.5;box-shadow:0 16px 32px rgba(37,99,235,.2)}.practice-options{border:1px dashed #bfd1e8;border-radius:16px;padding:16px;margin:16px 0;display:grid;grid-template-columns:1.4fr 1fr 1fr 1fr;gap:14px;align-items:end}.checkline{display:flex;gap:12px;align-items:center}.checkline input{width:auto}.checkline span{font-weight:400;color:#65748b}.compact{margin:0}.right{text-align:right}.exam-progress{display:flex;justify-content:space-between;font-weight:600;margin:22px 0}.question-card{background:linear-gradient(180deg,#ffffff,#fcfdff)}.question-card h2{font-size:24px;line-height:1.45;font-weight:500}.q-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:22px}.option{display:flex;gap:14px;align-items:flex-start;border:1px solid #d8e4f2;border-radius:16px;padding:18px;margin:12px 0;font-weight:400}.option-key{display:inline-block;min-width:28px;font-weight:600}.option input{width:auto;margin-top:3px}.option:has(input:checked){border-color:#2f67e8;background:#eef5ff}.practice-msg{margin-top:16px;background:#edf5ff;border-radius:14px;padding:14px;color:#1957d2;font-weight:600}.practice-msg.ok{color:#08783d}.practice-msg.bad{color:#d0222f}.nav-actions{display:flex;justify-content:space-between;align-items:stretch;gap:18px;margin-top:20px;padding:18px 20px;border:1px solid #d6e2f1;border-radius:22px;background:linear-gradient(180deg,rgba(255,255,255,.92),rgba(237,245,255,.98));box-shadow:0 18px 40px rgba(16,58,112,.08)}.nav-actions-card{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px;flex:1}.nav-btn{min-height:54px;border:1px solid #c7d7ef;background:#fff}.submit-btn{min-width:140px;min-height:54px;box-shadow:0 14px 28px rgba(47,103,232,.24)}.quick-nav{position:sticky;top:16px;align-self:start}.nav-grid{display:grid;grid-template-columns:repeat(5,1fr);gap:10px}.nav-grid button{height:44px;border:1px solid #cbd8ea;background:#fff;border-radius:13px;font-weight:500}.nav-grid button.active{border-color:#2f67e8;background:#edf4ff;color:#1957d2}.nav-grid button.answered{border-color:#22a85a;background:#ebfff2}.nav-grid button.passed{border-color:#16a34a;background:#dcfce7}.nav-grid button.failed{border-color:#dc2626;background:#fee2e2}.score-box{margin-top:14px;background:#edf5ff;border-radius:14px;padding:12px;color:#1957d2;font-weight:500}.legend{display:flex;gap:12px;flex-wrap:wrap;margin-top:14px;font-size:13px}.dot{display:inline-block;width:10px;height:10px;border-radius:50%;background:#cbd8ea;margin-right:6px}.dot.active{background:#2f67e8}.dot.answered{background:#22a85a}.big-score{font-size:52px;font-weight:900;color:#17358f}.result-wrap{min-height:100vh;padding:32px 16px}.result-card-wide{width:min(1120px,100%);margin:0 auto;background:#fff;border:1px solid #d6e2f1;border-radius:24px;padding:28px;box-shadow:0 28px 70px rgba(29,78,137,.12)}.pill.success{background:#dcfce7;color:#15803d}.summary-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;margin:24px 0}.summary-box{border-radius:18px;padding:18px}.summary-box span{display:block;font-size:14px;font-weight:700;color:#475569;margin-bottom:10px}.summary-box strong{font-size:34px;line-height:1.1}.summary-box.blue{background:#eff6ff}.summary-box.blue strong{color:#2563eb}.summary-box.green{background:#f0fdf4}.summary-box.green strong{color:#16a34a}.summary-box.orange{background:#fff7ed}.summary-box.orange strong{color:#ea580c}.summary-box.purple{background:#f5f3ff}.summary-box.purple strong{color:#7c3aed}.result-note{background:#f8fafc;border-radius:18px;padding:18px 20px;margin-bottom:24px}.result-note h2{margin:0 0 10px;font-size:18px}.result-note p{margin:0;color:#334155;line-height:1.6}.detail-head{margin-bottom:16px}.detail-head h2{margin-bottom:8px}.review-table-wrap{overflow:auto;border:1px solid #d6e2f1;border-radius:18px}.review-table{width:100%;border-collapse:collapse;background:#fff}.review-table th,.review-table td{padding:14px 12px;border-bottom:1px solid #e2e8f0;text-align:left;vertical-align:top}.review-table th{background:#f8fafc;font-size:14px}.review-table tr.ok{background:linear-gradient(90deg,#f0fdf4,#f7fff9)}.review-table tr.partial{background:linear-gradient(90deg,#fff7ed,#fffaf4)}.review-table tr.bad{background:linear-gradient(90deg,#fef2f2,#fff7f7)}.review-table tbody tr:hover{filter:brightness(.985)}.result-badge{display:inline-flex;align-items:center;padding:6px 12px;border-radius:999px;font-size:13px;font-weight:800;letter-spacing:.01em}.result-badge.ok{background:#dcfce7;color:#166534}.result-badge.partial{background:#ffedd5;color:#c2410c}.result-badge.bad{background:#fee2e2;color:#b91c1c}.cell-sub{margin-top:8px;color:#64748b;font-size:13px;font-weight:600}@media(max-width:900px){body{font-size:18px;padding-bottom:140px}.exam-shell{display:block;padding:18px 12px}.quick-nav{position:static;margin-top:16px}.exam-hero,.grid2,.start-hero{display:block}.start-highlight{max-width:none;margin-top:14px}.timer{min-width:0;padding:16px 18px}.timer strong{font-size:30px}.timer.timer-unlimited strong{font-size:24px}.practice-options{grid-template-columns:1fr}.nav-actions{position:sticky;bottom:10px;z-index:35;display:grid;gap:12px;padding:12px 12px calc(12px + env(safe-area-inset-bottom,0px));margin-top:18px;border-radius:24px;background:linear-gradient(180deg,rgba(255,255,255,.96),rgba(237,245,255,.98));box-shadow:0 18px 40px rgba(16,58,112,.16);backdrop-filter:blur(14px)}.nav-actions-card{grid-template-columns:repeat(3,minmax(0,1fr))}.nav-btn,.submit-btn{min-height:50px;padding:12px 10px;font-size:16px}.submit-btn{width:100%}.question-card h2{font-size:26px}.auth-card{padding:24px}.result-wrap{padding:18px 12px}.result-card-wide{padding:22px}.summary-grid{grid-template-columns:1fr 1fr}.summary-box strong{font-size:28px}}
</style>";
        }

        private HttpRequest ReadRequest(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024 * 256];
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0) return null;
            string raw = Encoding.UTF8.GetString(buffer, 0, read);
            int headerEnd = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            string header = headerEnd >= 0 ? raw.Substring(0, headerEnd) : raw;
            string body = headerEnd >= 0 ? raw.Substring(headerEnd + 4) : "";
            var lines = header.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var first = lines[0].Split(' ');
            int len = 0;
            foreach (var line in lines) if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) int.TryParse(line.Substring(15).Trim(), out len);
            while (Encoding.UTF8.GetByteCount(body) < len)
            {
                read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0) break;
                body += Encoding.UTF8.GetString(buffer, 0, read);
            }
            return new HttpRequest { Method = first.Length > 0 ? first[0].ToUpperInvariant() : "GET", Path = first.Length > 1 ? first[1].Split('?')[0] : "/", Body = body };
        }

        private void WriteResponse(TcpClient client, string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            var header = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nContent-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n");
            var stream = client.GetStream();
            stream.Write(header, 0, header.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        private Dictionary<string, string> ParseForm(string body)
        {
            var result = new Dictionary<string, string>();
            foreach (var pair in (body ?? "").Split('&'))
            {
                if (string.IsNullOrEmpty(pair)) continue;
                var p = pair.Split(new[] { '=' }, 2);
                result[Uri.UnescapeDataString(p[0].Replace('+', ' '))] = p.Length > 1 ? Uri.UnescapeDataString(p[1].Replace('+', ' ')) : "";
            }
            return result;
        }

        private Dictionary<string, string> ParseJsonMap(string json)
        {
            var map = new Dictionary<string, string>();
            json = (json ?? "").Trim().Trim('{', '}');
            if (json.Length == 0) return map;
            foreach (var part in json.Split(','))
            {
                var p = part.Split(new[] { ':' }, 2);
                if (p.Length == 2) map[p[0].Trim().Trim('"')] = p[1].Trim().Trim('"').Replace("\\\"", "\"");
            }
            return map;
        }

        private decimal CalculateAwardedPoints(DataRow q, string answer)
        {
            decimal points = D(q, "Points", 1);
            string type = NormalizeQuestionType(S(q, "QuestionType"));
            if (type == "FillBlank")
            {
                var evaluation = FillBlankScoringService.Evaluate(answer, S(q, "CorrectAnswer"));
                return Math.Round(points * evaluation.ScoreRatio, 2, MidpointRounding.AwayFromZero);
            }
            return Eq((answer ?? "").Trim(), S(q, "CorrectAnswer").Trim()) ? points : 0M;
        }

        private bool IsCorrect(DataRow q, string answer)
        {
            string type = NormalizeQuestionType(S(q, "QuestionType"));
            string correct = S(q, "CorrectAnswer");
            return type == "FillBlank" ? FillBlankScoringService.Evaluate(answer, correct).IsCorrect : Eq((answer ?? "").Trim(), (correct ?? "").Trim());
        }

        private static string NormalizeQuestionType(string type)
        {
            type = (type ?? "").Trim();
            if (Eq(type, "FillBlank") || Eq(type, "TuLuan") || Eq(type, "ShortAnswer")) return "FillBlank";
            return "MultipleChoice";
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)) return ip.ToString();
            }
            catch { }
            return "127.0.0.1";
        }

        private string ErrorPage(string title, string detail) { return Layout(title, "<main class='auth-wrap'><section class='auth-card'><h1>" + T(title) + "</h1><p class='muted'>" + T(detail) + "</p></section></main>"); }
        private string Plain(string text) { return "<pre>" + Enc(text) + "</pre>"; }
        private static string Get(Dictionary<string, string> d, string k) { return d != null && d.ContainsKey(k) ? d[k] : ""; }
        private static bool Has(DataRow r, string c) { return r.Table.Columns.Contains(c); }
        private static string S(DataRow r, string c) { return r != null && Has(r, c) && r[c] != DBNull.Value ? Convert.ToString(r[c]) : ""; }
        private static int I(DataRow r, string c, int def) { int v; return int.TryParse(S(r, c), out v) ? v : def; }
        private static decimal D(DataRow r, string c, decimal def) { if (r == null || !Has(r, c) || r[c] == DBNull.Value) return def; if (r[c] is decimal) return (decimal)r[c]; if (r[c] is double || r[c] is float) return Convert.ToDecimal(r[c]); if (r[c] is int || r[c] is long || r[c] is short || r[c] is byte) return Convert.ToDecimal(r[c]); decimal v; var s = Convert.ToString(r[c], CultureInfo.CurrentCulture); if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return v; if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v; return def; }
        private static bool B(DataRow r, string c, bool def) { if (!Has(r, c) || r[c] == DBNull.Value) return def; return Convert.ToBoolean(r[c]); }
        private static int ToInt(string s) { int v; return int.TryParse(s, out v) ? v : 0; }
        private static decimal ToDec(string s) { decimal v; return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0; }
        private static bool Eq(string a, string b) { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
        private static string First(string a, string b) { return string.IsNullOrWhiteSpace(a) ? b : a; }
        private static string Norm(string s) { return (s ?? "").Trim().ToLowerInvariant(); }
        private static string Sql(string s) { return (s ?? "").Replace("'", "''"); }
        private static string Enc(string s) { return WebUtility.HtmlEncode(s ?? ""); }
        private static string JsBool(bool value) { return value ? "true" : "false"; }
        private static string Js(string s) { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n") + "\""; }

        private static string T(string value)
        {
            var sb = new StringBuilder();
            foreach (char ch in value ?? "")
            {
                if (ch > 127) sb.Append("&#").Append((int)ch).Append(';');
                else sb.Append(WebUtility.HtmlEncode(ch.ToString()));
            }
            return sb.ToString();
        }

        private static string V(string value)
        {
            switch (value ?? "")
            {
                case "Co loi xay ra": return "C\u00f3 l\u1ed7i x\u1ea3y ra";
                case "Khong tim thay trang": return "Kh\u00f4ng t\u00ecm th\u1ea5y trang";
                case "Khong tim thay bai thi": return "Kh\u00f4ng t\u00ecm th\u1ea5y b\u00e0i thi";
                case "Ma phong hoac lien ket bai thi khong hop le.": return "M\u00e3 ph\u00f2ng ho\u1eb7c li\u00ean k\u1ebft b\u00e0i thi kh\u00f4ng h\u1ee3p l\u1ec7.";
                case "Bai thi truc tuyen": return "B\u00e0i thi tr\u1ef1c tuy\u1ebfn";
                case "Thoi gian lam bai": return "Th\u1eddi gian l\u00e0m b\u00e0i";
                case "phut": return "ph\u00fat";
                case "Ten dang nhap": return "T\u00ean \u0111\u0103ng nh\u1eadp";
                case "Mat khau tai khoan": return "M\u1eadt kh\u1ea9u t\u00e0i kho\u1ea3n";
                case "Mat khau bai thi": return "M\u1eadt kh\u1ea9u b\u00e0i thi";
                case "Vao lam bai": return "V\u00e0o l\u00e0m b\u00e0i";
                case "Thoi gian khong gioi han": return "Th\u1eddi gian kh\u00f4ng gi\u1edbi h\u1ea1n";
                case "Vao on khong can dang nhap": return "V\u00e0o \u00f4n kh\u00f4ng c\u1ea7n \u0111\u0103ng nh\u1eadp";
                case "Xac nhan thong tin truoc khi vao bai": return "X\u00e1c nh\u1eadn th\u00f4ng tin tr\u01b0\u1edbc khi v\u00e0o b\u00e0i";
                case "Co the doi tuy chon on tap truoc khi bat dau.": return "C\u00f3 th\u1ec3 \u0111\u1ed5i t\u00f9y ch\u1ecdn \u00f4n t\u1eadp tr\u01b0\u1edbc khi b\u1eaft \u0111\u1ea7u.";
                case "Kiem tra lai thong tin roi bat dau lam bai.": return "Ki\u1ec3m tra l\u1ea1i th\u00f4ng tin r\u1ed3i b\u1eaft \u0111\u1ea7u l\u00e0m b\u00e0i.";
                case "Thoi gian": return "Th\u1eddi gian";
                case "Khong gioi han": return "Kh\u00f4ng gi\u1edbi h\u1ea1n";
                case "Ban co the on tap thoai mai, khong bi dem nguoc.": return "B\u1ea1n c\u00f3 th\u1ec3 \u00f4n t\u1eadp tho\u1ea3i m\u00e1i, kh\u00f4ng b\u1ecb \u0111\u1ebfm ng\u01b0\u1ee3c.";
                case "Can canh nhip nho de giu tien do lam bai.": return "C\u1ea7n canh nh\u1ecbp nh\u1ecf \u0111\u1ec3 gi\u1eef ti\u1ebfn \u0111\u1ed9 l\u00e0m b\u00e0i.";
                case "Mat khau bai thi khong dung.": return "M\u1eadt kh\u1ea9u b\u00e0i thi kh\u00f4ng \u0111\u00fang.";
                case "Ten dang nhap hoac mat khau khong dung.": return "T\u00ean \u0111\u0103ng nh\u1eadp ho\u1eb7c m\u1eadt kh\u1ea9u kh\u00f4ng \u0111\u00fang.";
                case "Chua the vao bai thi": return "Ch\u01b0a th\u1ec3 v\u00e0o b\u00e0i thi";
                case "On luyen tren trinh duyet": return "\u00d4n luy\u1ec7n tr\u00ean tr\u00ecnh duy\u1ec7t";
                case "Lam bai tren trinh duyet": return "L\u00e0m b\u00e0i tr\u00ean tr\u00ecnh duy\u1ec7t";
                case "Sinh vien": return "Sinh vi\u00ean";
                case "Thoi gian con lai": return "Th\u1eddi gian c\u00f2n l\u1ea1i";
                case "Thong tin du thi": return "Th\u00f4ng tin d\u1ef1 thi";
                case "Ho ten": return "H\u1ecd t\u00ean";
                case "Nhap dung ho ten va lop de giao vien tong hop diem": return "Nh\u1eadp \u0111\u00fang h\u1ecd t\u00ean v\u00e0 l\u1edbp \u0111\u1ec3 gi\u00e1o vi\u00ean t\u1ed5ng h\u1ee3p \u0111i\u1ec3m";
                case "Lop": return "L\u1edbp";
                case "Xac nhan thong tin da nhap": return "X\u00e1c nh\u1eadn th\u00f4ng tin \u0111\u00e3 nh\u1eadp";
                case "Phai co lop truoc khi bat dau.": return "Ph\u1ea3i c\u00f3 l\u1edbp tr\u01b0\u1edbc khi b\u1eaft \u0111\u1ea7u.";
                case "Kiem tra thong tin truoc khi bat dau.": return "Ki\u1ec3m tra th\u00f4ng tin tr\u01b0\u1edbc khi b\u1eaft \u0111\u1ea7u.";
                case "Bat dau lam bai": return "B\u1eaft \u0111\u1ea7u l\u00e0m b\u00e0i";
                case "Cau": return "C\u00e2u";
                case "cau da tra loi": return "c\u00e2u \u0111\u00e3 tr\u1ea3 l\u1eddi";
                case "Cau truoc": return "C\u00e2u tr\u01b0\u1edbc";
                case "Xoa dap an": return "X\u00f3a \u0111\u00e1p \u00e1n";
                case "Cau tiep": return "C\u00e2u ti\u1ebfp";
                case "Nop bai": return "N\u1ed9p b\u00e0i";
                case "Dieu huong nhanh": return "\u0110i\u1ec1u h\u01b0\u1edbng nhanh";
                case "Bam vao so cau de chuyen nhanh. Mau xanh la da tra loi.": return "B\u1ea5m v\u00e0o s\u1ed1 c\u00e2u \u0111\u1ec3 chuy\u1ec3n nhanh. M\u00e0u xanh l\u00e0 \u0111\u00e3 tr\u1ea3 l\u1eddi.";
                case "Diem cong don hien tai": return "\u0110i\u1ec3m c\u1ed9ng d\u1ed3n hi\u1ec7n t\u1ea1i";
                case "Dang lam": return "\u0110ang l\u00e0m";
                case "Da tra loi": return "\u0110\u00e3 tr\u1ea3 l\u1eddi";
                case "Chua tra loi": return "Ch\u01b0a tr\u1ea3 l\u1eddi";
                case "Tuy chon on do nguoi lam bai tu chon": return "T\u00f9y ch\u1ecdn \u00f4n do ng\u01b0\u1eddi l\u00e0m b\u00e0i t\u1ef1 ch\u1ecdn";
                case "Sai toi da moi cau": return "Sai t\u1ed1i \u0111a m\u1ed7i c\u00e2u";
                case "Hien dap an": return "Hi\u1ec7n \u0111\u00e1p \u00e1n";
                case "Hien diem cong don": return "Hi\u1ec7n \u0111i\u1ec3m c\u1ed9ng d\u1ed3n";
                case "Tu chuyen cau khi dung": return "T\u1ef1 chuy\u1ec3n c\u00e2u khi \u0111\u00fang";
                case "Da nop bai": return "\u0110\u00e3 n\u1ed9p b\u00e0i";
                case "Ket qua bai thi": return "K\u1ebft qu\u1ea3 b\u00e0i thi";
                case "Diem cau hoi": return "\u0110i\u1ec3m c\u00e2u h\u1ecfi";
                case "Bai thi chua den gio mo.": return "B\u00e0i thi ch\u01b0a \u0111\u1ebfn gi\u1edd m\u1edf.";
                case "Bai thi da het han.": return "B\u00e0i thi \u0111\u00e3 h\u1ebft h\u1ea1n.";
                case "Tu luan ngan / dien dap an": return "T\u1ef1 lu\u1eadn ng\u1eafn / \u0111i\u1ec1n \u0111\u00e1p \u00e1n";
                case "Trac nghiem mot dap an": return "Tr\u1eafc nghi\u1ec7m m\u1ed9t \u0111\u00e1p \u00e1n";
                default: return value;
            }
        } 

        private class HttpRequest
        {
            public string Method;
            public string Path;
            public string Body;
        }
    }
}


























