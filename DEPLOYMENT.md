# PANDUAN DEPLOYMENT KE CLOUD
## Site Attendance Management System (.NET + PostgreSQL)

Panduan ini menjelaskan cara mempublikasikan aplikasi dan database ke layanan Cloud sehingga dapat diakses oleh administrator (**Vino Rifhendi Setiawan**) dari laptop, PC kantor, maupun smartphone melalui **satu URL publik**.

---

## 1. Topologi Arsitektur Cloud

```
[ Laptop Administrator ] ──┐
[ PC Kantor Site ]       ──┼──> [ Cloud Web App Service ] ──> [ Managed Cloud PostgreSQL ]
[ Smartphone Mobile ]    ──┘     (ASP.NET Core Web App)         (Neon / Supabase / Railway)
                                (Render / Railway / Azure)      (Single Source of Truth)
```

> **PENTING:** GitHub Pages **hanya** untuk file statis HTML/CSS. Backend ASP.NET Core dan PostgreSQL harus di-deploy ke Cloud Web Service & Managed Database.

---

## 2. Pilihan Layanan Managed PostgreSQL Cloud (Gratis & Cepat)

Anda dapat membuat database PostgreSQL di penyedia cloud gratis berikut:

### Opsi A: Neon (Serverless PostgreSQL - Direkomendasikan)
1. Buka [neon.tech](https://neon.tech) dan buat akun gratis.
2. Buat project baru: `site-attendance`.
3. Salin **Connection String** yang disediakan:
   ```text
   postgresql://username:password@ep-xyz-pooler.ap-southeast-1.aws.neon.tech/site_attendance_db?sslmode=require
   ```

### Opsi B: Supabase
1. Buka [supabase.com](https://supabase.com).
2. Buat project baru dan buka menu **Settings -> Database -> Connection String (URI)**.

---

## 3. Pilihan Layanan Hosting ASP.NET Core

### Opsi A: Railway (Sangat Mudah & Otomatis)
1. Buka [railway.app](https://railway.app).
2. Pilih **New Project -> Deploy from GitHub repo**.
3. Pilih repository GitHub ini.
4. Di tab **Variables**, tambahkan environment variable:
   - `DATABASE_URL`: Masukkan connection string PostgreSQL dari Neon / Supabase.
   - `ADMIN_INITIAL_PASSWORD`: `AdminSite2026!`
   - `ASPNETCORE_ENVIRONMENT`: `Production`
5. Railway akan otomatis mendeteksi `Dockerfile` dan melakukan deploy.

### Opsi B: Render.com
1. Buka [render.com](https://render.com).
2. Klik **New -> Web Service**.
3. Hubungkan repository GitHub.
4. Pilih **Runtime: Docker**.
5. Tambahkan Environment Variable:
   - `DATABASE_URL`: Connection string PostgreSQL Anda.
   - `ADMIN_INITIAL_PASSWORD`: Password awal admin Anda.

### Opsi C: Azure App Service
1. Buat **Web App** (Linux, .NET 10).
2. Hubungkan GitHub Actions CI/CD.
3. Atur **Application Settings**:
   - `ConnectionStrings__DefaultConnection`: `<connection-string>`

### Opsi D: VPS / Server Mandiri (Docker Compose)
Jika menggunakan server Ubuntu/Debian/Windows Server sendiri:
```bash
# Clone repository
git clone https://github.com/your-username/site-attendance.git
cd site-attendance

# Jalankan app + postgres dalam 1 perintah
docker compose up -d
```

---

## 4. Migrasi & Seeding Otomatis pada Startup

Aplikasi telah dilengkapi mekanisme **Auto-Migration** dan **Auto-Seeding**:
- Saat aplikasi pertama kali dijalankan di cloud dan terhubung ke PostgreSQL, Entity Framework Core secara otomatis:
  1. Menjalankan skrip migrasi tabel (`InitialCreate`).
  2. Menginisialisasi Role `Administrator`.
  3. Menginisialisasi akun administrator `vino.setiawan`.
  4. Menginisialisasi data 2 Engineer On Site (`Vino Rifhendi Setiawan` dan `Muhammad Setiawan`).
  5. Mengisi data absensi awal periode berjalan.

---

## 5. Akses Multi-Device

Setelah deploy berhasil:
1. Administrator membuka URL publik (contoh: `https://site-attendance.up.railway.app`).
2. Login menggunakan akun:
   - **Username**: `vino.setiawan`
   - **Password**: `AdminSite2026!` (atau sesuai `ADMIN_INITIAL_PASSWORD`).
3. Seluruh perubahan absensi yang dicatat dari smartphone saat inspeksi lapangan akan **langsung terlihat** di laptop atau komputer kantor secara real-time karena mengakses database PostgreSQL yang sama.

