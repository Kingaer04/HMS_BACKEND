using HMS.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HMS.Repository.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── DbSets ────────────────────────────────────────────────────
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientIdSequence> PatientIdSequences { get; set; }
        public DbSet<PatientAccessControl> PatientAccessControls { get; set; }
        public DbSet<AccessLog> AccessLogs { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<VitalInfo> VitalInfos { get; set; }
        public DbSet<VitalReading> VitalReadings { get; set; }
        public DbSet<AllergyInfo> AllergyInfos { get; set; }
        public DbSet<DoctorNote> DoctorNotes { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<LabRequest> LabRequests { get; set; }
        public DbSet<LabTestResult> LabTestResults { get; set; }
        public DbSet<LabTestCatalogue> LabTestCatalogues { get; set; }
        public DbSet<HospitalVisit> HospitalVisits { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentLineItem> PaymentLineItems { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<DoctorProfile> DoctorProfiles { get; set; }
        public DbSet<ReceptionistProfile> ReceptionistProfiles { get; set; }
        public DbSet<LabTechnicianProfile> LabTechnicianProfiles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<TemporaryPatientRecord> TemporaryPatientRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── Hospital ──────────────────────────────────────────────
            builder.Entity<Hospital>(e =>
            {
                e.HasKey(h => h.Id);
                e.HasIndex(h => h.HospitalUID).IsUnique();
                e.Property(h => h.HospitalUID).IsRequired().HasMaxLength(50);
                e.Property(h => h.Name).IsRequired().HasMaxLength(200);
                e.Property(h => h.Country).HasDefaultValue("Nigeria");
            });

            // ── ApplicationUser ───────────────────────────────────────
            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                e.Property(u => u.LastName).IsRequired().HasMaxLength(100);

                e.HasOne(u => u.Hospital)
                 .WithMany(h => h.Staff)
                 .HasForeignKey(u => u.HospitalId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Patient ───────────────────────────────────────────────
            builder.Entity<Patient>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasIndex(p => p.HmsPatientId).IsUnique();
                e.Property(p => p.HmsPatientId).IsRequired().HasMaxLength(20);
                e.HasIndex(p => p.PhoneNumber);
                e.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
                e.Property(p => p.LastName).IsRequired().HasMaxLength(100);
                e.Property(p => p.Country).HasDefaultValue("Nigeria");

                // Patient → OriginHospital (many patients per hospital)
                e.HasOne(p => p.OriginHospital)
                 .WithMany(h => h.RegisteredPatients)
                 .HasForeignKey(p => p.OriginHospitalId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Patient → ApplicationUser (optional — only if patient uses app)
                e.HasOne(p => p.User)
                 .WithOne(u => u.PatientRecord)
                 .HasForeignKey<Patient>(p => p.UserId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── PatientIdSequence ─────────────────────────────────────
            builder.Entity<PatientIdSequence>(e =>
            {
                e.HasKey(s => s.Id);
                e.HasIndex(s => s.Year).IsUnique();
            });

            // ── PatientAccessControl ──────────────────────────────────
            builder.Entity<PatientAccessControl>(e =>
            {
                e.HasKey(a => a.Id);

                e.HasOne(a => a.Patient)
                 .WithOne(p => p.AccessControl)
                 .HasForeignKey<PatientAccessControl>(a => a.PatientId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── AccessLog ─────────────────────────────────────────────
            builder.Entity<AccessLog>(e =>
            {
                e.HasKey(a => a.Id);

                e.HasOne(a => a.AccessControl)
                 .WithMany(ac => ac.AccessLogs)
                 .HasForeignKey(a => a.PatientAccessControlId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── MedicalRecord ─────────────────────────────────────────
            builder.Entity<MedicalRecord>(e =>
            {
                e.HasKey(m => m.Id);

                e.HasOne(m => m.Patient)
                 .WithOne(p => p.MedicalRecord)
                 .HasForeignKey<MedicalRecord>(m => m.PatientId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── VitalInfo ─────────────────────────────────────────────
            builder.Entity<VitalInfo>(e =>
            {
                e.HasKey(v => v.Id);

                e.HasOne(v => v.MedicalRecord)
                 .WithOne(m => m.VitalInfo)
                 .HasForeignKey<VitalInfo>(v => v.MedicalRecordId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Ignore computed property — not mapped to DB
                e.Ignore(v => v.BMI);
            });

            // ── VitalReading ──────────────────────────────────────────
            builder.Entity<VitalReading>(e =>
            {
                e.HasKey(vr => vr.Id);

                e.HasOne(vr => vr.VitalInfo)
                 .WithMany(v => v.VitalReadings)
                 .HasForeignKey(vr => vr.VitalInfoId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── AllergyInfo ───────────────────────────────────────────
            builder.Entity<AllergyInfo>(e =>
            {
                e.HasKey(a => a.Id);

                e.HasOne(a => a.MedicalRecord)
                 .WithOne(m => m.AllergyInfo)
                 .HasForeignKey<AllergyInfo>(a => a.MedicalRecordId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── DoctorNote ────────────────────────────────────────────
            builder.Entity<DoctorNote>(e =>
            {
                e.HasKey(d => d.Id);

                e.HasOne(d => d.MedicalRecord)
                 .WithMany(m => m.DoctorNotes)
                 .HasForeignKey(d => d.MedicalRecordId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Ignore computed property
                e.Ignore(d => d.IsEditable);
            });

            // ── Prescription ──────────────────────────────────────────
            builder.Entity<Prescription>(e =>
            {
                e.HasKey(p => p.Id);

                e.HasOne(p => p.MedicalRecord)
                 .WithMany(m => m.Prescriptions)
                 .HasForeignKey(p => p.MedicalRecordId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── HospitalVisit ─────────────────────────────────────────
            builder.Entity<HospitalVisit>(e =>
            {
                e.HasKey(v => v.Id);

                e.HasOne(v => v.Patient)
                 .WithMany(p => p.Visits)
                 .HasForeignKey(v => v.PatientId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(v => v.Hospital)
                 .WithMany()
                 .HasForeignKey(v => v.HospitalId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(v => v.MedicalRecord)
                 .WithMany(m => m.VisitHistory)
                 .HasForeignKey(v => v.MedicalRecordId)
                 .OnDelete(DeleteBehavior.Restrict);

                // ── The fix for your error ────────────────────────────
                // HospitalVisit is the DEPENDENT side — it holds AppointmentId
                // Appointment is the PRINCIPAL — exists independently
                e.HasOne(v => v.Appointment)
                 .WithOne(a => a.ResultingVisit)
                 .HasForeignKey<HospitalVisit>(v => v.AppointmentId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Appointment ───────────────────────────────────────────
            builder.Entity<Appointment>(e =>
            {
                e.HasKey(a => a.Id);

                e.HasOne(a => a.Patient)
                 .WithMany(p => p.Appointments)
                 .HasForeignKey(a => a.PatientId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.Hospital)
                 .WithMany()
                 .HasForeignKey(a => a.HospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── LabRequest ────────────────────────────────────────────
            builder.Entity<LabRequest>(e =>
            {
                e.HasKey(l => l.Id);

                e.HasOne(l => l.MedicalRecord)
                 .WithMany(m => m.LabRequests)
                 .HasForeignKey(l => l.MedicalRecordId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(l => l.Hospital)
                 .WithMany()
                 .HasForeignKey(l => l.HospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── LabTestResult ─────────────────────────────────────────
            builder.Entity<LabTestResult>(e =>
            {
                e.HasKey(r => r.Id);

                e.HasOne(r => r.LabRequest)
                 .WithMany(l => l.TestResults)
                 .HasForeignKey(r => r.LabRequestId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── LabTestCatalogue ──────────────────────────────────────────────────
            builder.Entity<LabTestCatalogue>(e =>
            {
                e.HasKey(c => c.Id);

                // Fix decimal warning
                e.Property(c => c.Price).HasPrecision(18, 2);

                e.HasOne(c => c.Hospital)
                 .WithMany(h => h.LabCatalogue)
                 .HasForeignKey(c => c.HospitalId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Payment ───────────────────────────────────────────────────────────
            builder.Entity<Payment>(e =>
            {
                e.HasKey(p => p.Id);

                e.Ignore(p => p.TotalAmount);
                e.Ignore(p => p.Balance);

                // Fix decimal warnings — specify precision explicitly
                e.Property(p => p.ConsultationFee).HasPrecision(18, 2);
                e.Property(p => p.LabFees).HasPrecision(18, 2);
                e.Property(p => p.MedicationFees).HasPrecision(18, 2);
                e.Property(p => p.OtherFees).HasPrecision(18, 2);
                e.Property(p => p.Discount).HasPrecision(18, 2);
                e.Property(p => p.NHISCoverage).HasPrecision(18, 2);
                e.Property(p => p.AmountPaid).HasPrecision(18, 2);

                e.HasOne(p => p.Visit)
                 .WithOne(v => v.Payment)
                 .HasForeignKey<Payment>(p => p.VisitId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.Hospital)
                 .WithMany()
                 .HasForeignKey(p => p.HospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── PaymentLineItem ───────────────────────────────────────────────────
            builder.Entity<PaymentLineItem>(e =>
            {
                e.HasKey(l => l.Id);

                e.Ignore(l => l.Total);

                // Fix decimal warning
                e.Property(l => l.Amount).HasPrecision(18, 2);

                e.HasOne(l => l.Payment)
                 .WithMany(p => p.LineItems)
                 .HasForeignKey(l => l.PaymentId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Department ────────────────────────────────────────────
            builder.Entity<Department>(e =>
            {
                e.HasKey(d => d.Id);

                e.HasOne(d => d.Hospital)
                 .WithMany(h => h.Departments)
                 .HasForeignKey(d => d.HospitalId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── DoctorProfile ─────────────────────────────────────────
            builder.Entity<DoctorProfile>(e =>
            {
                e.HasKey(d => d.Id);

                e.HasOne(d => d.User)
                 .WithOne(u => u.DoctorProfile)
                 .HasForeignKey<DoctorProfile>(d => d.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(d => d.Hospital)
                 .WithMany()
                 .HasForeignKey(d => d.HospitalId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.Department)
                 .WithMany(dep => dep.Doctors)
                 .HasForeignKey(d => d.DepartmentId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── ReceptionistProfile ───────────────────────────────────
            builder.Entity<ReceptionistProfile>(e =>
            {
                e.HasKey(r => r.Id);

                e.HasOne(r => r.User)
                 .WithOne(u => u.ReceptionistProfile)
                 .HasForeignKey<ReceptionistProfile>(r => r.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(r => r.Hospital)
                 .WithMany()
                 .HasForeignKey(r => r.HospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── LabTechnicianProfile ──────────────────────────────────
            builder.Entity<LabTechnicianProfile>(e =>
            {
                e.HasKey(l => l.Id);

                e.HasOne(l => l.User)
                 .WithOne(u => u.LabTechnicianProfile)
                 .HasForeignKey<LabTechnicianProfile>(l => l.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(l => l.Hospital)
                 .WithMany()
                 .HasForeignKey(l => l.HospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Notification ──────────────────────────────────────────
            builder.Entity<Notification>(e =>
            {
                e.HasKey(n => n.Id);

                e.HasOne(n => n.RelatedVisit)
                 .WithMany(v => v.Notifications)
                 .HasForeignKey(n => n.RelatedVisitId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(n => n.RelatedAppointment)
                 .WithMany(a => a.Notifications)
                 .HasForeignKey(n => n.RelatedAppointmentId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── AuditLog ──────────────────────────────────────────────
            builder.Entity<AuditLog>(e =>
            {
                e.HasKey(a => a.Id);
                e.HasIndex(a => a.Timestamp);
                e.HasIndex(a => a.UserId);
            });

            // ── TemporaryPatientRecord ────────────────────────────────
            builder.Entity<TemporaryPatientRecord>(e =>
            {
                e.HasKey(t => t.Id);

                e.HasOne(t => t.Visit)
                 .WithMany()
                 .HasForeignKey(t => t.VisitId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Seed Roles ────────────────────────────────────────────
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "HospitalAdmin", NormalizedName = "HOSPITALADMIN" },
                new IdentityRole { Id = "2", Name = "Doctor", NormalizedName = "DOCTOR" },
                new IdentityRole { Id = "3", Name = "Receptionist", NormalizedName = "RECEPTIONIST" },
                new IdentityRole { Id = "4", Name = "LabTechnician", NormalizedName = "LABTECHNICIAN" },
                new IdentityRole { Id = "5", Name = "Patient", NormalizedName = "PATIENT" }
            );
        }
    }
}