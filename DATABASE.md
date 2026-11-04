# DOKUMENTASI DATABASE POSTGRESQL
## Site Attendance Management System (Manage Service SIG – Site Rembang)

Aplikasi ini menggunakan **PostgreSQL** sebagai *single source of truth*. Seluruh transaksi pencatatan absensi, data engineer, autentikasi pengguna, dan jejak audit log disimpan secara persisten di database relasional PostgreSQL.

---

## 1. Arsitektur Data

```
[ Laptop Administrator ] ──┐
[ PC Kantor Site ]       ──┼──> [ ASP.NET Core Web App ] ──> [ PostgreSQL Database ]
[ Smartphone Mobile ]    ──┘     (Entity Framework Core)      (Single Source of Truth)
```

- Ketika administrator mengakses sistem dari perangkat manapun (laptop, PC, smartphone), seluruh aktivitas membaca dan menulis ke instance PostgreSQL yang sama.
- Data absensi **TIDAK** disimpan di LocalStorage browser, session file, atau JSON lokal.

---

## 2. Struktur Tabel & Skema Relasional

### A. Tabel `Employees` (Master Engineer On Site)
Menyimpan data identitas resmi teknisi/engineer yang bertugas di Site Rembang.

| Kolom | Tipe Data | Constraint | Keterangan |
| :--- | :--- | :--- | :--- |
| `Id` | `integer` | PK, Auto Increment | Identifier unik internal |
| `EmployeeCode` | `character varying(50)` | UNIQUE, NOT NULL | Kode personil resmi (contoh: `EOS-001`, `EOS-002`) |
| `FullName` | `character varying(150)` | NOT NULL | Nama lengkap personil |
| `Position` | `character varying(100)` | NOT NULL | Posisi/jabatan kerja |
| `Site` | `character varying(100)` | NOT NULL | Lokasi penugasan (Default: `Site Rembang`) |
| `IsActive` | `boolean` | NOT NULL, Default `true` | Status keaktifan personil |
| `CreatedAt` | `timestamp with time zone` | NOT NULL | Waktu pendaftaran (UTC) |

### B. Tabel `Attendances` (Transaksi Rekam Absensi)
Menyimpan data catatan absensi harian personil.

| Kolom | Tipe Data | Constraint | Keterangan |
| :--- | :--- | :--- | :--- |
| `Id` | `integer` | PK, Auto Increment | Identifier unik transaksi |
| `EmployeeId` | `integer` | FK -> `Employees(Id)`, NOT NULL | Hubungan ke master Engineer |
| `AttendanceDate` | `date` | NOT NULL | Tanggal absensi (format ISO: `yyyy-MM-dd`) |
| `CheckIn` | `time without time zone` | NULLABLE | Jam masuk kerja (HH:mm:ss) |
| `CheckOut` | `time without time zone` | NULLABLE | Jam pulang kerja (HH:mm:ss) |
| `Status` | `integer` | NOT NULL | Enum: `1=Hadir, 2=Izin, 3=Sakit, 4=Cuti, 5=Libur` |
| `Description` | `character varying(500)` | NULLABLE | Uraian kegiatan/alasan izin |
| `TotalWorkHours` | `numeric(5,2)` | NOT NULL, Default `0.00` | Akumulasi jam kerja harian |
| `CreatedAt` | `timestamp with time zone` | NOT NULL | Waktu input record (UTC) |
| `UpdatedAt` | `timestamp with time zone` | NULLABLE | Waktu update terakhir (UTC) |

#### Database Constraints:
- **Unique Composite Index**:
  ```sql
  CREATE UNIQUE INDEX "IX_Attendance_Employee_Date_Unique"
  ON "Attendances" ("EmployeeId", "AttendanceDate");
  ```
  *Mencegah terjadinya pencatatan ganda untuk engineer dan tanggal yang sama di level database engine.*

### C. Tabel `AuditLogs` (Jejak Rekam Keamanan)
Mencatat seluruh aksi administrator (Create, Update, Delete, Export, Backup, Restore).

| Kolom | Tipe Data | Constraint | Keterangan |
| :--- | :--- | :--- | :--- |
| `Id` | `integer` | PK, Auto Increment | Primary key log |
| `UserId` | `character varying(100)` | NULLABLE | Username pelaksana aksi |
| `Action` | `character varying(50)` | NOT NULL | `CREATE`, `UPDATE`, `DELETE`, `LOGIN`, `EXPORT`, dll. |
| `EntityName` | `character varying(100)` | NOT NULL | Nama entitas yang terpengaruh |
| `EntityId` | `character varying(50)` | NULLABLE | ID entitas |
| `Description` | `character varying(1000)`| NOT NULL | Detail deskripsi kejadian |
| `CreatedAt` | `timestamp with time zone` | NOT NULL | Waktu kejadian (UTC) |

### D. Tabel Autentikasi ASP.NET Core Identity
- `AspNetUsers`: Menyimpan akun administrator (Password hash PBKDF2/Argon2, email, status konfirmasi).
- `AspNetRoles`: Role administrator.
- `AspNetUserRoles`: Pemetaan role ke pengguna.

---

## 3. Konvensi Waktu & Timezone
- Seluruh kolom timestamp disimpan dalam **UTC** di PostgreSQL (`timestamp with time zone`).
- Aplikasi menampilkan waktu kepada pengguna dalam zona **Asia/Jakarta (WIB, UTC+7)**.
- Kolom `AttendanceDate` menggunakan tipe data SQL standard `date` (tanpa offset waktu) sehingga konsisten di belahan dunia manapun.

---

## 4. Backup & Restore Data
1. **Via UI Aplikasi**:
   - Buka menu **Pengaturan & Backup**.
   - Klik **Unduh Backup** untuk menghasilkan file JSON terenkripsi dan terstruktur berisi seluruh tabel.
   - Upload file cadangan untuk melakukan restorasi instan.
2. **Via PostgreSQL Native CLI (pg_dump)**:
   ```bash
   # Backup (Dump)
   pg_dump -h <host> -U <user> -d site_attendance_db -F c -b -v -f site_attendance_backup.dump

   # Restore
   pg_restore -h <host> -U <user> -d site_attendance_db -v site_attendance_backup.dump
   ```

