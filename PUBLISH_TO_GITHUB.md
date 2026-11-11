# Đưa source lên GitHub (repository Public)

## 1. Tạo repository trống

1. Đăng nhập GitHub và mở https://github.com/new
2. Repository name: `QuizManagement`
3. Chọn **Public**.
4. Không chọn tạo sẵn README, `.gitignore` hoặc LICENSE (source đã có các file cần thiết).
5. Nhấn **Create repository**.

## 2. Đẩy source từ Windows bằng Git

Giải nén `QuizManagement-GitHub-Public.zip`. Mở PowerShell tại thư mục vừa giải nén, sau đó chạy lần lượt:

```powershell
git init
git add .
git status
git commit -m "Initial commit: QuizManagement"
git branch -M main
git remote add origin https://github.com/lionfly1802/QuizManagement.git
git push -u origin main
```

Nếu Git yêu cầu đăng nhập, hoàn tất cửa sổ xác thực Git Credential Manager/GitHub trong trình duyệt. Không nhập token vào source code hoặc gửi token trong cuộc trò chuyện.

## 3. Kiểm tra sau khi đẩy

- Mở `https://github.com/lionfly1802/QuizManagement`.
- Kiểm tra các thư mục `QuizManagement/BUS`, `DAL`, `GUI` và file `QuizManagement.sln` đã xuất hiện.
- Vào **Settings → General** để xác nhận repository đang ở chế độ Public.

## Lưu ý

Repository này là nơi lưu mã nguồn. Phương án A chưa triển khai website lên GitHub Pages; ứng dụng và web exam server vẫn chạy trên máy Windows của bạn. Không bật GitHub Pages cho project WinForms này vì Pages không chạy được C# server/SQL Server.
