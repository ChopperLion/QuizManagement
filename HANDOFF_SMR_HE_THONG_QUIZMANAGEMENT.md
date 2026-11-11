# HANDOFF SMR - HE THONG QUIZMANAGEMENT

## 1. Muc dich tai lieu

Tai lieu nay dung de ban giao nhanh hien trang he thong `QuizManagement` cho nguoi phu trach viet bao cao Word phan mem. Noi dung duoi day bam sat code trong repo hien tai, uu tien mo ta dung kien truc, module, luong nghiep vu va cac diem can nhan manh khi dua vao bao cao.

Ngay chot thong tin: `2026-07-03`

## 2. Tong quan he thong

`QuizManagement` la ung dung quan ly de thi trac nghiem viet bang `C# WinForms` tren `.NET Framework 4.7.2`, ket noi `SQL Server`. He thong phuc vu 3 nhom nguoi dung:

1. `Admin`: quan tri tai khoan va theo doi thong ke tong quan.
2. `Teacher`: tao de, quan ly ngan hang cau hoi, chia se de cho sinh vien, xem bang diem va thong ke.
3. `Student`: tim de, vao lam bai, xem lich su, va tao bo de on tap ca nhan.

Diem dac biet cua he thong hien tai:

1. Ngoai giao dien WinForms, he thong con co mot `web exam server` noi bo de sinh vien co the lam bai bang trinh duyet.
2. He thong ho tro hai che do de:
   - `Exam`: de thi thong thuong.
   - `Practice`: de on luyen, co them cac tuy chon hien dap an, gioi han so lan sai, auto next, khong gioi han thoi gian, khong can dang nhap.
3. He thong tu `ensure schema` khi mo mot so man hinh, nghia la tu dong bo sung cot/bang SQL neu database chua du cau truc moi.

## 3. Nen tang ky thuat

- Ngon ngu: `C#`
- Giao dien: `Windows Forms`
- Framework: `.NET Framework 4.7.2`
- CSDL: `SQL Server`
- Kieu project: `WinExe`
- Thu vien dung san chu yeu: `System.Windows.Forms`, `System.Data`, `System.IO.Compression`, `System.Windows.Forms.DataVisualization`

Entry point cua app:

- `QuizManagement/Program.cs`
- App nhan tham so dong lenh; neu tham so dau la exam link hop le thi se dua vao `frmLogin` de xu ly vao bai sau dang nhap.

## 4. Cau truc ma nguon muc lon

Project duoc chia thanh 3 tang chinh:

### 4.1. `GUI/`

Chua toan bo WinForms va UserControl:

- `frmLogin`: man hinh dang nhap.
- `frmAdmin`: giao dien quan tri he thong.
- `frmTeacher`: giao dien giao vien.
- `frmStudent`: giao dien sinh vien.
- `frmExam`: man hinh lam bai tren desktop.
- `frmScoreReport`: bang diem, xem ket qua, xuat diem.
- `frmCreateExam`: UserControl quan ly bo de, cau hoi, tao de, chia se de, nhap file.
- `ucTrangChuTeacher`, `ucThongKeTeacher`: tong quan va thong ke cho giao vien.
- `QuestionEditDialog`, `FillBlankReviewDialog`, `StudentExamIdentityPrompt`: cac dialog phu tro.

### 4.2. `BUS/`

Chua nghiep vu chinh:

- `StudentExamWebServer`: web server noi bo cho lam bai tren trinh duyet.
- `ExamLinkShareService`: tao/quan ly link LAN va link cong khai qua tunnel.
- `QuestionImportService`: nhap cau hoi tu `xlsx/csv/txt/tsv`.
- `FillBlankScoringService`: cham cau tra loi ngan/dien khuyet.
- `ScoreExportService`: xuat bang diem ra file `xlsx`.
- `UserBUS`, `QuestionBUS`, `ExamShareLinkService`: nghiep vu dang nhap va xu ly bo tro.

### 4.3. `DAL/`

Tang truy cap du lieu:

- `Database`: lop thuc thi SQL truc tiep, mo ket noi, query, non-query, va cac ham `Ensure...` de nang schema.
- `UserDAL`, `QuestionDAL`, `ExamDAL`: cac DAL chuyen biet.

## 5. Luong su dung tong the

### 5.1. Dang nhap va phan quyen

Nguoi dung vao `frmLogin`, nhap username/password. Sau khi `UserBUS.Login(...)` tra ve thong tin user:

1. `RoleID = 1` mo `frmAdmin`
2. `RoleID = 2` mo `frmTeacher`
3. `RoleID = 3` mo `frmStudent`

Neu app duoc mo bang exam link, link nay duoc giu trong `pendingExamLink` de xu ly o luong sinh vien.

### 5.2. Luong giao vien

Giao vien la nhan vat trung tam cua he thong:

1. Quan ly bo de va bo cau hoi.
2. Tao de moi, sua de, nhan ban de, xoa de.
3. Nhap cau hoi tu file mau.
4. Cau hinh de thi hoac de on.
5. Chia se de qua link LAN hoac link Internet.
6. Theo doi bang diem, lich su nop bai va thong ke.

Khi mo `frmTeacher`, he thong:

1. Dam bao cac cot diem, ownership, thong tin sinh vien da ton tai trong DB.
2. Thu khoi dong `StudentExamWebServer`.
3. Nap giao dien tong quan, quan ly de, bang diem, thong ke.

### 5.3. Luong sinh vien

Sinh vien trong `frmStudent` co cac nhom chuc nang:

1. Dashboard tong quan.
2. Dang ky/vao de thi.
3. Xem lich su ket qua.
4. Quan ly thong tin tai khoan.
5. Quan ly bo de on tap ca nhan thong qua `ucManageExam` o che do student practice.

Sinh vien co the lam bai theo 2 cach:

1. Lam bai trong app desktop qua `frmExam`.
2. Lam bai bang trinh duyet qua link do giao vien chia se, do `StudentExamWebServer` phuc vu.

### 5.4. Luong admin

`frmAdmin` phu trach:

1. Quan ly danh sach tai khoan.
2. Them/sua/xoa/reset mat khau.
3. Xem thong ke tong so user, de thi, cau hoi, ket qua.

## 6. Chuc nang nghiep vu chinh

### 6.1. Quan ly de thi

Module chinh nam trong `frmCreateExam.cs` va `frmTeacher.cs`.

Kha nang nghiep vu:

1. Tao bo de moi.
2. Them cau hoi tu ngan hang.
3. Tao moi cau hoi.
4. Tim kiem, loc, phan trang danh sach de va danh sach cau hoi.
5. Sao chep de.
6. Xoa de.
7. Chia deu diem hoac cham diem thu cong tung cau.

Bo de co the chua:

1. Thong tin co ban nhu ten de, thoi gian, mat khau de.
2. Cac tham so mo rong nhu start/end time, max attempts, random question, room code, exam link.
3. Tham so danh rieng cho practice mode.

### 6.2. Ngan hang cau hoi

He thong ho tro it nhat 2 loai cau hoi:

1. `MultipleChoice`
2. `FillBlank`

Moi cau hoi co the co:

- Noi dung cau hoi
- Dap an A/B/C/D
- Dap an dung
- Diem tung cau
- Nguoi tao
- Trang thai publish

Voi `FillBlank`, he thong cho phep de trong A/B/C/D va cham dua tren noi dung dap an dung.

### 6.3. Nhap cau hoi tu file

`QuestionImportService` ho tro:

1. Doc file `xlsx`, `csv`, `txt`, `tsv`.
2. Xem truoc ket qua import.
3. Kiem tra dong hop le/khong hop le.
4. Chen cau hoi moi hoac tai su dung cau hoi da ton tai.
5. Tu dong lien ket vao de cu the neu co `linkExamId`.
6. Sinh file mau de giao vien dien du lieu.

Logic import co tinh an toan:

1. Chay transaction.
2. Neu loi se rollback toan bo dot import de tranh du lieu do dang.
3. Ghi so dong hop le, da import, da lien ket, da bo qua va danh sach loi.

### 6.4. Lam bai tren desktop

`frmExam` la man hinh lam bai WinForms:

1. Hien noi dung cau hoi, bo dap an, dong ho dem nguoc.
2. Chuyen cau, danh dau xem lai, nop bai.
3. Ho tro multiple choice va fill blank.
4. Ho tro practice mode voi diem cong don, khoa cau sau khi dung/het luot, phan hoi ngay tren man hinh.

Che do practice trong desktop co cac tuy chon:

1. So lan sai toi da moi cau.
2. Hien dap an.
3. Hien diem cong don.
4. Tu dong qua cau khi tra loi dung.

### 6.5. Lam bai tren trinh duyet

`StudentExamWebServer` la thanh phan rat quan trong cua he thong hien tai.

Co che hoat dong:

1. App WinForms khoi dong mot `TcpListener`.
2. Server uu tien port `5000`, neu ban se tu dong fallback den `5010`.
3. Server tao URL dang `http://<local-ip>:<port>/exam/<roomCode>`.
4. Sinh vien vao link, dang nhap neu de yeu cau, sau do lam bai tren giao dien HTML/CSS/JS duoc server render truc tiep.

Khung URL/phuc vu:

1. `GET /exam/{room}`: vao trang de.
2. `POST /exam/{room}/login`: dang nhap vao de.
3. `POST /exam/{room}/submit`: nop bai.
4. `GET /exam/{room}/ping`: keep-alive/ping phuc vu kiem tra ket noi.

Kha nang nghiep vu cua web exam:

1. Ho tro `Exam` va `Practice`.
2. Co the bo qua dang nhap neu de on cho phep `PracticeNoLoginRequired`.
3. Co the cho phep thoi gian vo han `PracticeUnlimitedTime`.
4. Co the bat sinh vien nhap ho ten/lop truoc khi bat dau.
5. Co the cho phep sinh vien tu tuy chon cach on truoc khi start.
6. Luu ket qua vao `Results` va chi tiet tung cau vao `ExamAttemptAnswers`.

Cham diem web exam:

1. Tinh tong diem raw theo `Points` cua tung cau.
2. Quy doi theo `ScoreScale` cua de, mac dinh la thang 10.
3. Luu `AwardedPoints` cho tung cau, nhat la quan trong voi fill blank va practice.

### 6.6. Chia se de qua LAN/Internet

`ExamLinkShareService` phu trach xay dung link chia se:

1. Link LAN: dua tren `StudentExamWebServer.BuildExamUrl(roomCode)`.
2. Link cong khai: dua tren `CloudflarePublicBaseUrl` luu trong `Settings`.
3. He thong co the tu thu chay `cloudflared` de tao tunnel tam thoi.

Luu y quan trong de dua vao bao cao:

1. Tunnel cong khai hien tai mang tinh tam thoi, khong phai domain co dinh.
2. He thong co canh bao neu bai thi dai, nhat la tu khoang `45` phut va `90` phut tro len.
3. Port web khong nen mo ta co dinh la `5000`; phai ghi dung la `5000-5010` voi co che fallback.

### 6.7. Bang diem, lich su va thong ke

Phan nay nam o `frmScoreReport`, `frmStudent`, `ucThongKeTeacher`, `ucTrangChuTeacher`.

Kha nang:

1. Xem ket qua da nop.
2. Tong hop so cau dung, so cau da tra loi, diem quy doi.
3. Xem lich su lam bai cua sinh vien.
4. Xuat bang diem lop ra file `xlsx` bang `ScoreExportService`.
5. Hien bieu do/thong ke cho giao vien.

## 7. Co so du lieu va mo rong schema

He thong hien tai khong chi dung cau truc DB co ban ban dau, ma con tu mo rong bang/cot khi can.

### 7.1. Cac bang/chuyen muc du lieu chinh

Co the suy ra cac bang nghiep vu chinh:

1. `Users`
2. `Roles`
3. `Questions`
4. `Exams`
5. `ExamQuestions`
6. `Results`
7. `ExamAttemptAnswers`

### 7.2. Cac cot mo rong quan trong

`Database.cs` dang bo sung cac nhom cot sau:

1. Nhom diem va loai cau hoi:
   - `Questions.Points`
   - `Questions.QuestionType`
   - `ExamQuestions.Points`

2. Nhom ownership/publish:
   - `Exams.CreatedByUserID`
   - `Questions.CreatedByUserID`
   - `Questions.IsPublishedToServer`
   - `Exams.IsPublishedToServer`

3. Nhom truy cap de:
   - `Exams.ExamPassword`
   - `Exams.ExamMode`
   - `Exams.StartTime`
   - `Exams.EndTime`
   - `Exams.MaxAttempts`
   - `Exams.IsRandomQuestion`
   - `Exams.RandomQuestionCount`
   - `Exams.RoomCode`
   - `Exams.ExamLink`

4. Nhom practice mode:
   - `Exams.ScoreScale`
   - `Exams.PracticeMaxWrongPerQuestion`
   - `Exams.PracticeShowAnswer`
   - `Exams.PracticeShowPointGain`
   - `Exams.PracticeAutoNextOnCorrect`
   - `Exams.RequireClassBeforeStart`
   - `Exams.AllowStudentEditClass`
   - `Exams.PracticeAllowStudentOptions`
   - `Exams.PracticeNoLoginRequired`
   - `Exams.PracticeUnlimitedTime`

5. Nhom danh tinh sinh vien va ket qua:
   - `Users.ClassName`
   - `Users.AvatarPath`
   - `Results.StudentFullName`
   - `Results.StudentClass`

6. Bang chi tiet lan lam bai:
   - `ExamAttemptAnswers`
   - Luu `QuestionOrder`, `QuestionContent`, `UserAnswer`, `CorrectAnswer`, `IsCorrect`, `QuestionPoints`, `AwardedPoints`, `CreatedAt`

### 7.3. Luu y bao cao

Khi viet bao cao, nen mo ta rang:

1. He thong co kha nang tu dong cap nhat schema o muc co ban khi app chay.
2. Tuy nhien van phu thuoc quyen DDL tren SQL Server; neu khong du quyen, app co the bo qua migration va mot so chuc nang nang cao can chay SQL script bo sung.

## 8. Cac diem nghiep vu noi bat can dua vao bao cao

### 8.1. Practice mode

Day la diem moi va de viet thanh gia tri san pham:

1. De on tap tach biet voi de thi that.
2. Cho phep chon cach on linh hoat.
3. Cham ngay tung cau.
4. Hien tien do va diem cong don.
5. Co the cho hoc sinh vao on ma khong can tai khoan.
6. Co the bo thoi gian gioi han.

### 8.2. Cham fill blank co diem mot phan

He thong khong chi cham dung/sai don gian:

1. So sanh cau tra loi sau khi chuan hoa text.
2. Co xu ly muc do gan dung.
3. Co the cap `AwardedPoints` mot phan thay vi chi 0 hoac diem toi da.

### 8.3. Theo doi chi tiet tung cau da nop

Bang `ExamAttemptAnswers` giup:

1. Xem lai tung cau sinh vien da tra loi.
2. Phan tich cau nao dung/sai.
3. Phuc vu bao cao, xem bai lam, va mo rong thong ke sau nay.

### 8.4. Chia se de qua trinh duyet

He thong da vuot qua mo hinh WinForms dong kin:

1. Giao vien van quan ly tren desktop.
2. Sinh vien co the lam bai bang browser qua LAN hoac link cong khai tam thoi.
3. Day la diem rat nen neu trong bao cao vi the hien kha nang mo rong va trien khai thuc te.

## 9. Han che hien tai can ghi trung thuc

Nen viet trung thuc de bao cao phan mem sat hien trang:

1. Tang du lieu hien tai van con nhieu cau SQL dang noi chuoi truc tiep, chua la ORM.
2. Web exam server la server noi bo tu viet, chua phai web app ASP.NET tach rieng.
3. Link cong khai phu thuoc tunnel tam thoi neu chua co domain/ha tang co dinh.
4. Mot so nang schema duoc goi luc runtime, nen moi truong SQL can du quyen thay doi cau truc hoac duoc cap nhat truoc.
5. Trong ma nguon co dau hieu text encoding chua dong nhat o mot so file giao dien; khong anh huong lon den kien truc nhung nen biet khi trich chup code/man hinh.

## 10. Goi y bo cuc de dua vao bao cao Word

Nguoi viet Word co the tai cau truc noi dung nhu sau:

1. Gioi thieu de tai
2. Muc tieu he thong
3. Cong nghe su dung
4. Phan tich tac nhan `Admin/Teacher/Student`
5. So do kien truc 3 tang `GUI - BUS - DAL - SQL Server`
6. Mo ta CSDL va cac bang chinh
7. Mo ta cac chuc nang:
   - Dang nhap, phan quyen
   - Quan ly de thi
   - Quan ly ngan hang cau hoi
   - Nhap cau hoi tu file
   - Lam bai desktop
   - Lam bai web
   - Chia se link
   - Bang diem va thong ke
8. Diem moi cua he thong:
   - Practice mode
   - Fill blank scoring
   - ExamAttemptAnswers
   - Browser-based exam
9. Han che va huong phat trien

## 11. Cac file nen trich dan neu can viet sau hon

Neu nguoi lam bao cao can dao sau vao code, uu tien doc cac file nay:

- `QuizManagement/Program.cs`
- `QuizManagement/DAL/Database.cs`
- `QuizManagement/GUI/frmLogin.cs`
- `QuizManagement/GUI/frmTeacher.cs`
- `QuizManagement/GUI/frmStudent.cs`
- `QuizManagement/GUI/frmExam.cs`
- `QuizManagement/GUI/frmCreateExam.cs`
- `QuizManagement/BUS/StudentExamWebServer.cs`
- `QuizManagement/BUS/ExamLinkShareService.cs`
- `QuizManagement/BUS/QuestionImportService.cs`
- `QuizManagement/BUS/ScoreExportService.cs`

## 12. Ket luan handoff

Tinh den ngay `2026-07-03`, `QuizManagement` khong con la ung dung tao de trac nghiem co ban. He thong da phat trien thanh mot nen tang quan ly de thi co 3 vai tro, ho tro de thi va de on, cham diem theo tung cau, ghi nhan chi tiet bai lam, nhap/xuat du lieu, va chia se bai lam sang trinh duyet qua web server noi bo.

Neu can viet ngan gon trong bao cao, co the tom tat mot cau nhu sau:

`QuizManagement la he thong thi trac nghiem WinForms ket hop SQL Server, trong do giao vien quan ly de va chia se bai lam, sinh vien co the lam bai tren desktop hoac trinh duyet, con he thong luu duoc ket qua va chi tiet tung cau de phuc vu danh gia va thong ke.`
