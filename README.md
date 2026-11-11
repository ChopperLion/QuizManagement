# QuizManagement

Ứng dụng quản lý và tổ chức thi trắc nghiệm, xây dựng bằng C# Windows Forms trên .NET Framework 4.7.2 và SQL Server.

> **Lưu ý về triển khai:** Repository GitHub này dùng để lưu trữ và quản lý mã nguồn. Ở phương án A, ứng dụng WinForms và web exam server vẫn chạy trên máy Windows của người tổ chức thi. GitHub Pages không chạy được ứng dụng WinForms/C# server hoặc SQL Server.

## Chức năng chính

- Quản lý tài khoản theo vai trò Admin, Teacher và Student.
- Tạo và quản lý đề thi, ngân hàng câu hỏi.
- Hỗ trợ câu hỏi trắc nghiệm và điền khuyết.
- Nhập câu hỏi từ XLSX/CSV/TXT/TSV.
- Làm bài trên ứng dụng Windows hoặc trình duyệt.
- Chia sẻ bài thi qua LAN hoặc Cloudflare Tunnel.
- Chấm điểm, xem lịch sử và xuất báo cáo điểm.
- Hỗ trợ chế độ Exam và Practice.

## Công nghệ

- C# Windows Forms
- .NET Framework 4.7.2
- Microsoft SQL Server
- TCP listener nội bộ phục vụ giao diện thi trên trình duyệt
- Cloudflare Tunnel (tùy chọn) để tạo đường dẫn Internet

## Yêu cầu

- Windows 10/11
- Visual Studio có workload .NET desktop development
- .NET Framework 4.7.2 Developer Pack/Targeting Pack
- SQL Server hoặc SQL Server Express
- Database `QuizManagement` đã được tạo và nạp schema cần thiết
- `cloudflared` nếu muốn tạo link Internet bằng Cloudflare Tunnel

## Mở và chạy

1. Clone repository hoặc tải source code về máy Windows.
2. Mở `QuizManagement.sln` bằng Visual Studio.
3. Mở `QuizManagement/DAL/Database.cs` và kiểm tra chuỗi kết nối SQL Server. Mặc định source dùng `localhost\SQLEXPRESS`, database `QuizManagement` và Windows Authentication.
4. Nếu SQL Server của bạn dùng tên instance khác, hãy sửa `Data Source` cho đúng máy của bạn.
5. Đảm bảo database đã được tạo và có schema phù hợp. File `SQLQuery1.sql` là một script migration bổ sung schema; hãy sao lưu database trước khi chạy script trên dữ liệu thật.
6. Build và chạy project `QuizManagement`.
7. Để chia sẻ bài thi ra Internet theo cơ chế hiện có, giữ ứng dụng mở, khởi động web exam server và dùng chức năng Cloudflare Tunnel trong phần mềm. Máy chủ, SQL Server và kết nối Internet trên máy tổ chức phải hoạt động trong suốt thời gian thi.

## Chia sẻ bài thi: giới hạn của phương án A

- GitHub **không** lưu kết quả thi thay cho SQL Server trong kiến trúc hiện tại.
- GitHub Pages **không** host được web server C# đang nằm trong ứng dụng WinForms.
- Link Cloudflare Tunnel dạng `trycloudflare.com` là link tạm thời; có thể thay đổi khi tạo tunnel mới.
- Nếu tắt ứng dụng, tắt máy chủ hoặc mất kết nối Internet, thí sinh có thể không truy cập/nộp bài được.
- Không nên coi cấu hình này là hạ tầng thi có SLA hoặc bảo đảm tải lớn.

## Bảo mật khi dùng repository công khai

- Không commit dữ liệu thật của học sinh/sinh viên, file database, bản sao lưu, file thực thi hoặc thông tin đăng nhập.
- Không đưa mật khẩu, token, connection string có thông tin xác thực vào source code.
- Repository công khai đồng nghĩa mọi người có thể đọc và tải mã nguồn.
- Cấu hình SQL Server trong source đã được đổi từ tên máy cá nhân sang `localhost\SQLEXPRESS`; hãy cấu hình lại theo môi trường chạy của bạn.

## Cấu trúc

- `QuizManagement/GUI`: giao diện WinForms.
- `QuizManagement/BUS`: nghiệp vụ, web exam server và chia sẻ link.
- `QuizManagement/DAL`: truy cập SQL Server.
- `SQLQuery1.sql`: script migration schema.
- `HANDOFF_SMR_HE_THONG_QUIZMANAGEMENT.md`: ghi chú kiến trúc và luồng nghiệp vụ.

## Giấy phép

Chưa chỉ định giấy phép. Khi chưa có LICENSE, người khác có thể xem mã nguồn công khai nhưng không mặc nhiên được cấp quyền sử dụng, sửa đổi hoặc phân phối theo một giấy phép mã nguồn mở.
