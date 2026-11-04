# SITE ATTENDANCE MANAGEMENT SYSTEM
### .NET 10 LTS + PostgreSQL Edition

Aplikasi web administrasi enterprise untuk mengelola, memantau, dan menghasilkan laporan resmi absensi personil **Engineer On Site** pada **Manage Service SIG Site Rembang**.

Aplikasi ini dirancang dengan standar arsitektur monolitik modular profesional, menggunakan **PostgreSQL** sebagai *single source of truth* persisten sehingga dapat diakses secara sinkron dari berbagai perangkat (laptop, PC kantor, maupun smartphone).

---

## 1. Identitas Resmi & Penanggung Jawab

| Posisi / Peran | Nama | Jabatan & Instansi |
| :--- | :--- | :--- |
| **Administrator & Leader** | **Vino Rifhendi Setiawan** | Leader / Engineer On Site (Manage Service SIG) |
| **Engineer On Site** | **Muhammad Setiawan** | Engineer On Site (Manage Service SIG) |
| **Mengetahui** | **Dina Frinda Saragih** | Project Manager MS WiFi |
| **Menyetujui** | **Dwi Mulyantoro** | IT Site Infrastructure Officer, PT Semen Gresik (Persero) Tbk. |
| **Lokasi Operasional** | **Site Rembang** | Jln. Raya Rembang - Blora KM 17, Rembang, Jawa Tengah |

---

## 2. Technology Stack

- **Framework**: .NET 10 LTS
- **Backend & Web**: C#, ASP.NET Core Razor Pages
- **ORM & Data**: Entity Framework Core Code-First
- **Database Engine**: PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`
- **Autentikasi & Keamanan**: ASP.NET Core Identity, BCrypt/PBKDF2 Password Hashing, Proteksi CSRF, Anti-forgery tokens, HTTPS Enforced
- **Frontend UI/UX**: Razor Pages, HTML5, CSS3, Bootstrap 5, Lucide Icons, Clean Corporate Dashboard (Dominan Putih, Slate, dan Navy Blue)
- **Mesin Cetak & Ekspor**:
  - **QuestPDF**: Dokumen PDF Resmi format A4 siap cetak dengan 3 kolom tanda tangan manual.
  - **ClosedXML**: Spreadsheet Excel (`.xlsx`) berformat rapi dengan styling korporat dan formula rekapitulasi.
  - **CsvHelper**: Ekspor data tabular CSV standar RFC-4180.
- **Testing**: xUnit, Microsoft.EntityFrameworkCore.InMemory (6/6 tests passing 100%).

---

## 3. Fitur Utama Sistem

### A. Dashboard Korporat Dinamis
- KPI Cards dinamis dari PostgreSQL: Total Engineer, Total Hadir, Total Izin, Total Sakit, Total Cuti, Total Libur, Total Jam Kerja.
- Ringkasan performa kehadiran dan jam kerja per masing-masing engineer.
- Filter tanggal kustom (mendukung rentang apa saja, misal 1–30 November 2026 atau 15 September–14 Oktober 2026).
- Tabel aktivitas absensi terkini dan feed audit log sistem.

### B. Attendance Management (CRUD Lengkap)
- Input absensi: Tanggal, Nama Engineer, Status Kehadiran, Jam Masuk, Jam Pulang, Keterangan.
- Pilihan status: **Hadir, Izin, Sakit, Cuti, Libur**.
- Validasi bisnis:
  - Format jam kerja HH:mm (WIB).
  - Jam masuk dan pulang wajib diisi jika status Hadir (otomatis menghitung total jam kerja).
  - Status selain Hadir tidak mewajibkan jam kerja.
  - Validasi urutan waktu: Jam pulang tidak boleh lebih awal dari jam masuk.
  - **Pencegahan duplikasi**: Mencegah pencatatan ganda untuk engineer dan tanggal yang sama (divalidasi di Service Layer dan Unique Database Constraint PostgreSQL).
  - Dialog konfirmasi sebelum penghapusan data.
  - Notifikasi toast sukses dan gagal.

### C. Laporan Absensi Resmi & Tanda Tangan Fisik
Format laporan formal sesuai standar:
```
ABSENSI ENGINEER ON SITE - MANAGE SERVICE SIG
Lokasi: Site Rembang | Periode: [Tanggal Awal] s/d [Tanggal Akhir]
```
Tabel data: `No | Tanggal | Hari | Jam Masuk | Jam Pulang | Total Jam | Status | Keterangan`
Dilengkapi 3 blok tanda tangan resmi:
1. **DIBUAT OLEH**: Vino Rifhendi Setiawan (Administrator / Leader Engineer On Site)
2. **MENGETAHUI**: Dina Frinda Saragih (Project Manager MS WiFi)
3. **MENYETUJUI**: Dwi Mulyantoro (IT Site Infrastructure Officer PT Semen Gresik (Persero) Tbk.)
*Menyediakan ruang tanda tangan manual fisik (tidak menggunakan tanda tangan digital palsu).*

### D. Multi-Format Export
- **PDF Resmi per Engineer**: File PDF A4 terpisah untuk masing-masing personil.
- **PDF Gabungan**: Seluruh engineer dalam satu dokumen PDF terstruktur.
- **Excel (`.xlsx`)**: Berisi lembar kerja dengan grid dan format korporat.
- **CSV (`.csv`)**: Data mentah untuk integrasi sistem.
- Penamaan file otomatis mengikuti nama personil dan periode laporan.

### E. Backup & Restore Database
- Fitur ekspor basis data ke file cadangan JSON terstruktur.
- Fitur restorasi instan langsung melalui antarmuka web.

### F. Future Analytics Layer (Persiapan Bot AI)
Service layer terisolasi (`IAnalyticsService`) telah siap untuk integrasi bot analitik di masa mendatang:
- `GetMonthlyAttendanceSummaryAsync`
- `GetEmployeeAttendanceHistoryAsync`
- `GetAttendanceStatisticsAsync`
- `GetTotalWorkingHoursAsync`
- `GetAttendanceTrendAsync`

---

## 4. Struktur Proyek

```
We-recap/
├── SiteAttendance/                   # Proyek Utama ASP.NET Core Razor Pages
│   ├── Configuration/                # Kelas konfigurasi
│   ├── Data/                         # ApplicationDbContext, DbInitializer (Auto-Seeding)
│   ├── DTOs/                         # Data Transfer Objects untuk Analitik & Backup
│   ├── Migrations/                   # EF Core Code-First PostgreSQL Migrations
│   ├── Models/                       # Domain Entities (Employee, Attendance, AuditLog, User)
│   ├── Pages/                        # Razor Pages
│   │   ├── Account/                  # Login, Logout, AccessDenied
│   │   ├── Attendance/               # Index (CRUD Absensi), Create, Edit
│   │   ├── Employees/                # Index (Kelola Engineer), Create, Edit
│   │   ├── Reports/                  # Formal Report View & Export Handler
│   │   ├── Settings/                 # Backup/Restore & Audit Log Viewer
│   │   ├── Shared/                   # _Layout.cshtml (Sidebar & Topbar)
│   │   └── Index.cshtml              # Dashboard Utama
│   ├── Reports/                      # QuestPdfReportDocument (Template PDF A4 Resmi)
│   ├── Services/                     # AttendanceService, ExportService, BackupService, dll.
│   ├── ViewModels/                   # DashboardViewModel, ReportViewModel, InputModel
│   ├── wwwroot/                      # CSS Korporat, JS, Icons
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs
├── SiteAttendance.Tests/             # Automated Unit Testing Suite (xUnit)
│   └── AttendanceTests.cs
├── .github/workflows/ci-cd.yml       # GitHub Actions Automated CI/CD
├── .env.example                      # Contoh variabel lingkungan
├── .gitignore
├── DATABASE.md                       # Dokumentasi skema PostgreSQL & constraints
├── DEPLOYMENT.md                     # Panduan publikasi ke layanan Cloud
├── Dockerfile                        # Kontainerisasi produksi
├── docker-compose.yml                # Orkestrasi 1-klik App + PostgreSQL
└── README.md
```

---

## 5. Cara Menjalankan Aplikasi

### Opsi 1: Menjalankan Secara Lokal dengan .NET CLI

1. Pastikan runtime .NET 10 LTS terinstal.
2. Siapkan connection string PostgreSQL di `appsettings.json` atau environment variable `DATABASE_URL`.
3. Jalankan aplikasi:
   ```bash
   cd SiteAttendance
   dotnet run
   ```
4. Buka browser di `http://localhost:5000` atau URL yang ditampilkan di terminal.

### Opsi 2: Menggunakan Docker Compose (Direkomendasikan)
Menjalankan Web App beserta PostgreSQL Server dalam satu perintah:
```bash
docker compose up -d
```
Aplikasi langsung dapat diakses di: `http://localhost:8080`.

---

## 6. Kredensial Login Administrator Default

- **Username**: `vino.setiawan`
- **Password**: `AdminSite2026!`

*(Password dapat dikonfigurasi melalui environment variable `ADMIN_INITIAL_PASSWORD`).*

---

## 7. Hasil Pengujian Unit Test

Seluruh logika bisnis telah diuji secara otomatis menggunakan xUnit:
```
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6 - SiteAttendance.Tests.dll (net10.0)
```
- Test 1: Pembuatan master Engineer & pencegahan kode ganda.
- Test 2: Pembuatan absensi Hadir & akurasi kalkulasi jam kerja.
- Test 3: Pencegahan duplikasi absensi tanggal yang sama.
- Test 4: Penanganan status non-Hadir tanpa jam kerja.
- Test 5: Akurasi akumulasi data Analytics Service.
- Test 6: Integritas ekspor dokumen PDF A4 resmi, ClosedXML Excel, dan CSV.
- Test 7: Integritas siklus ekspor backup JSON dan restore data.

