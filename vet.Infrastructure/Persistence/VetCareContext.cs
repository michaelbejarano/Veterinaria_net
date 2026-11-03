using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using vet.Domain.Entities;

namespace vet.Infrastructure.Persistence;

public partial class VetCareContext : DbContext
{
    public VetCareContext(DbContextOptions<VetCareContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AppointmentReschedule> AppointmentReschedules { get; set; }

    public virtual DbSet<AppointmentService> AppointmentServices { get; set; }

    public virtual DbSet<BatchStock> BatchStocks { get; set; }

    public virtual DbSet<Breed> Breeds { get; set; }

    public virtual DbSet<CashMovement> CashMovements { get; set; }

    public virtual DbSet<CashRegister> CashRegisters { get; set; }

    public virtual DbSet<CashSession> CashSessions { get; set; }

    public virtual DbSet<CashSummary> CashSummaries { get; set; }

    public virtual DbSet<Commission> Commissions { get; set; }

    public virtual DbSet<CommissionRule> CommissionRules { get; set; }

    public virtual DbSet<CommissionSettlement> CommissionSettlements { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Consultation> Consultations { get; set; }

    public virtual DbSet<DepositApplication> DepositApplications { get; set; }

    public virtual DbSet<DepositForfeiture> DepositForfeitures { get; set; }

    public virtual DbSet<Diagnosis> Diagnoses { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }

    public virtual DbSet<EmployeeService> EmployeeServices { get; set; }

    public virtual DbSet<InventoryMovement> InventoryMovements { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Owner> Owners { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Pet> Pets { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductBatch> ProductBatches { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalaryPayment> SalaryPayments { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleItem> SaleItems { get; set; }

    public virtual DbSet<ScheduleBlock> ScheduleBlocks { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceSupply> ServiceSupplies { get; set; }

    public virtual DbSet<SettlementItem> SettlementItems { get; set; }

    public virtual DbSet<Species> Species { get; set; }

    public virtual DbSet<Treatment> Treatments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Visit> Visits { get; set; }

    public virtual DbSet<VisitService> VisitServices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("btree_gist");

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("appointments_pkey");

            entity.ToTable("appointments", "vetcare");

            entity.HasIndex(e => new { e.EmployeeId, e.StartAt }, "ix_appointments_employee_start");

            entity.HasIndex(e => new { e.PetId, e.StartAt }, "ix_appointments_pet_start");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DepositPercent)
                .HasPrecision(5, 2)
                .HasDefaultValue(30m)
                .HasColumnName("deposit_percent");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.PaymentExpiresAt).HasColumnName("payment_expires_at");
            entity.Property(e => e.PetId).HasColumnName("pet_id");
            entity.Property(e => e.RequiredDeposit)
                .HasPrecision(12, 2)
                .HasColumnName("required_deposit");
            entity.Property(e => e.ReservedPrice)
                .HasPrecision(12, 2)
                .HasColumnName("reserved_price");
            entity.Property(e => e.StartAt).HasColumnName("start_at");
            entity.Property(e => e.Status)
                .HasMaxLength(24)
                .HasDefaultValueSql("'PENDING_PAYMENT'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Employee).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appointments_employee_id_fkey");

            entity.HasOne(d => d.Pet).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appointments_pet_id_fkey");
        });

        modelBuilder.Entity<AppointmentReschedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("appointment_reschedules_pkey");

            entity.ToTable("appointment_reschedules", "vetcare");

            entity.HasIndex(e => e.AppointmentId, "uq_pending_reschedule")
                .IsUnique()
                .HasFilter("((status)::text = 'PENDING_PAYMENT'::text)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Fee)
                .HasPrecision(12, 2)
                .HasDefaultValue(10m)
                .HasColumnName("fee");
            entity.Property(e => e.NewEnd).HasColumnName("new_end");
            entity.Property(e => e.NewStart).HasColumnName("new_start");
            entity.Property(e => e.PaymentExpiresAt).HasColumnName("payment_expires_at");
            entity.Property(e => e.PreviousEnd).HasColumnName("previous_end");
            entity.Property(e => e.PreviousStart).HasColumnName("previous_start");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING_PAYMENT'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Appointment).WithOne(p => p.AppointmentReschedule)
                .HasForeignKey<AppointmentReschedule>(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appointment_reschedules_appointment_id_fkey");
        });

        modelBuilder.Entity<AppointmentService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("appointment_services_pkey");

            entity.ToTable("appointment_services", "vetcare");

            entity.HasIndex(e => new { e.AppointmentId, e.ServiceId }, "appointment_services_appointment_id_service_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AgreedPrice)
                .HasPrecision(12, 2)
                .HasColumnName("agreed_price");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentServices)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appointment_services_appointment_id_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.AppointmentServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appointment_services_service_id_fkey");
        });

        modelBuilder.Entity<BatchStock>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("batch_stock", "vetcare");

            entity.Property(e => e.BatchCode)
                .HasMaxLength(100)
                .HasColumnName("batch_code");
            entity.Property(e => e.BatchId).HasColumnName("batch_id");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Stock)
                .HasPrecision(14, 3)
                .HasColumnName("stock");
        });

        modelBuilder.Entity<Breed>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("breeds_pkey");

            entity.ToTable("breeds", "vetcare");

            entity.HasIndex(e => new { e.Id, e.SpeciesId }, "breeds_id_species_id_key").IsUnique();

            entity.HasIndex(e => new { e.SpeciesId, e.Name }, "breeds_species_id_name_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SpeciesId).HasColumnName("species_id");

            entity.HasOne(d => d.Species).WithMany(p => p.Breeds)
                .HasForeignKey(d => d.SpeciesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("breeds_species_id_fkey");
        });

        modelBuilder.Entity<CashMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cash_movements_pkey");

            entity.ToTable("cash_movements", "vetcare");

            entity.HasIndex(e => e.PaymentId, "cash_movements_payment_id_key").IsUnique();

            entity.HasIndex(e => new { e.CashSessionId, e.Date }, "ix_cash_movements_session");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CashSessionId).HasColumnName("cash_session_id");
            entity.Property(e => e.Category)
                .HasMaxLength(30)
                .HasColumnName("category");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Method)
                .HasMaxLength(20)
                .HasColumnName("method");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Reference)
                .HasMaxLength(120)
                .HasColumnName("reference");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .HasColumnName("type");

            entity.HasOne(d => d.CashSession).WithMany(p => p.CashMovements)
                .HasForeignKey(d => d.CashSessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cash_movements_cash_session_id_fkey");

            entity.HasOne(d => d.Payment).WithOne(p => p.CashMovement)
                .HasForeignKey<CashMovement>(d => d.PaymentId)
                .HasConstraintName("cash_movements_payment_id_fkey");
        });

        modelBuilder.Entity<CashRegister>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cash_registers_pkey");

            entity.ToTable("cash_registers", "vetcare");

            entity.HasIndex(e => new { e.CompanyId, e.Name }, "cash_registers_company_id_name_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.HasOne(d => d.Company).WithMany(p => p.CashRegisters)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cash_registers_company_id_fkey");
        });

        modelBuilder.Entity<CashSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cash_sessions_pkey");

            entity.ToTable("cash_sessions", "vetcare");

            entity.HasIndex(e => e.CashRegisterId, "uq_open_cash_session")
                .IsUnique()
                .HasFilter("((status)::text = 'OPEN'::text)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CashRegisterId).HasColumnName("cash_register_id");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.ClosedByUserId).HasColumnName("closed_by_user_id");
            entity.Property(e => e.CountedCash)
                .HasPrecision(12, 2)
                .HasColumnName("counted_cash");
            entity.Property(e => e.OpenedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("opened_at");
            entity.Property(e => e.OpenedByUserId).HasColumnName("opened_by_user_id");
            entity.Property(e => e.OpeningFloat)
                .HasPrecision(12, 2)
                .HasColumnName("opening_float");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValueSql("'OPEN'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.CashRegister).WithOne(p => p.CashSession)
                .HasForeignKey<CashSession>(d => d.CashRegisterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cash_sessions_cash_register_id_fkey");

            entity.HasOne(d => d.ClosedByUser).WithMany(p => p.CashSessionClosedByUsers)
                .HasForeignKey(d => d.ClosedByUserId)
                .HasConstraintName("cash_sessions_closed_by_user_id_fkey");

            entity.HasOne(d => d.OpenedByUser).WithMany(p => p.CashSessionOpenedByUsers)
                .HasForeignKey(d => d.OpenedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cash_sessions_opened_by_user_id_fkey");
        });

        modelBuilder.Entity<CashSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("cash_summary", "vetcare");

            entity.Property(e => e.CashInflows).HasColumnName("cash_inflows");
            entity.Property(e => e.CashOutflows).HasColumnName("cash_outflows");
            entity.Property(e => e.CashRegisterId).HasColumnName("cash_register_id");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.CountedCash)
                .HasPrecision(12, 2)
                .HasColumnName("counted_cash");
            entity.Property(e => e.ExpectedCash).HasColumnName("expected_cash");
            entity.Property(e => e.OpenedAt).HasColumnName("opened_at");
            entity.Property(e => e.OpeningFloat)
                .HasPrecision(12, 2)
                .HasColumnName("opening_float");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasColumnName("status");
        });

        modelBuilder.Entity<Commission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("commissions_pkey");

            entity.ToTable("commissions", "vetcare");

            entity.HasIndex(e => e.VisitServiceId, "commissions_visit_service_id_key").IsUnique();

            entity.HasIndex(e => new { e.EmployeeId, e.Status }, "ix_commissions_employee");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.AppliedType)
                .HasMaxLength(12)
                .HasColumnName("applied_type");
            entity.Property(e => e.AppliedValue)
                .HasPrecision(12, 2)
                .HasColumnName("applied_value");
            entity.Property(e => e.CalculationBase)
                .HasPrecision(12, 2)
                .HasColumnName("calculation_base");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.RuleId).HasColumnName("rule_id");
            entity.Property(e => e.Status)
                .HasMaxLength(14)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.VisitServiceId).HasColumnName("visit_service_id");

            entity.HasOne(d => d.Employee).WithMany(p => p.Commissions)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("commissions_employee_id_fkey");

            entity.HasOne(d => d.Rule).WithMany(p => p.Commissions)
                .HasForeignKey(d => d.RuleId)
                .HasConstraintName("commissions_rule_id_fkey");

            entity.HasOne(d => d.VisitService).WithOne(p => p.Commission)
                .HasForeignKey<Commission>(d => d.VisitServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("commissions_visit_service_id_fkey");
        });

        modelBuilder.Entity<CommissionRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("commission_rules_pkey");

            entity.ToTable("commission_rules", "vetcare");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.EmployeeServiceId).HasColumnName("employee_service_id");
            entity.Property(e => e.Type)
                .HasMaxLength(12)
                .HasColumnName("type");
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
            entity.Property(e => e.ValidTo).HasColumnName("valid_to");
            entity.Property(e => e.Value)
                .HasPrecision(12, 2)
                .HasColumnName("value");

            entity.HasOne(d => d.EmployeeService).WithMany(p => p.CommissionRules)
                .HasForeignKey(d => d.EmployeeServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("commission_rules_employee_service_id_fkey");
        });

        modelBuilder.Entity<CommissionSettlement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("commission_settlements_pkey");

            entity.ToTable("commission_settlements", "vetcare");

            entity.HasIndex(e => new { e.EmployeeId, e.Date }, "commission_settlements_employee_id_date_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.Status)
                .HasMaxLength(12)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Employee).WithMany(p => p.CommissionSettlements)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("commission_settlements_employee_id_fkey");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("companies_pkey");

            entity.ToTable("companies", "vetcare");

            entity.HasIndex(e => e.Ruc, "companies_ruc_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CertificadoSecretRef).HasColumnName("certificado_secret_ref");
            entity.Property(e => e.CertificatePath).HasColumnName("certificate_path");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasColumnName("department");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.Environment)
                .HasMaxLength(12)
                .HasDefaultValueSql("'BETA'::character varying")
                .HasColumnName("environment");
            entity.Property(e => e.LegalName)
                .HasMaxLength(200)
                .HasColumnName("legal_name");
            entity.Property(e => e.LocalCode)
                .HasMaxLength(10)
                .HasColumnName("local_code");
            entity.Property(e => e.Province)
                .HasMaxLength(100)
                .HasColumnName("province");
            entity.Property(e => e.Ruc)
                .HasMaxLength(11)
                .HasColumnName("ruc");
            entity.Property(e => e.SolSecretRef).HasColumnName("sol_secret_ref");
            entity.Property(e => e.SolUser)
                .HasMaxLength(100)
                .HasColumnName("sol_user");
            entity.Property(e => e.TradeName)
                .HasMaxLength(200)
                .HasColumnName("trade_name");
            entity.Property(e => e.Ubigeo)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("ubigeo");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Urbanization)
                .HasMaxLength(150)
                .HasColumnName("urbanization");
        });

        modelBuilder.Entity<Consultation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("consultations_pkey");

            entity.ToTable("consultations", "vetcare");

            entity.HasIndex(e => e.VisitId, "consultations_visit_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Anamnesis).HasColumnName("anamnesis");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.PhysicalExam).HasColumnName("physical_exam");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.VeterinarianId).HasColumnName("veterinarian_id");
            entity.Property(e => e.VisitId).HasColumnName("visit_id");
            entity.Property(e => e.WeightKg)
                .HasPrecision(8, 3)
                .HasColumnName("weight_kg");

            entity.HasOne(d => d.Veterinarian).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.VeterinarianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("consultations_veterinarian_id_fkey");

            entity.HasOne(d => d.Visit).WithOne(p => p.Consultation)
                .HasForeignKey<Consultation>(d => d.VisitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("consultations_visit_id_fkey");
        });

        modelBuilder.Entity<DepositApplication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("deposit_applications_pkey");

            entity.ToTable("deposit_applications", "vetcare");

            entity.HasIndex(e => new { e.PaymentId, e.SaleId }, "deposit_applications_payment_id_sale_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.SaleId).HasColumnName("sale_id");

            entity.HasOne(d => d.Payment).WithMany(p => p.DepositApplications)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("deposit_applications_payment_id_fkey");

            entity.HasOne(d => d.Sale).WithMany(p => p.DepositApplications)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("deposit_applications_sale_id_fkey");
        });

        modelBuilder.Entity<DepositForfeiture>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("deposit_forfeitures_pkey");

            entity.ToTable("deposit_forfeitures", "vetcare");

            entity.HasIndex(e => e.PaymentId, "deposit_forfeitures_payment_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(30)
                .HasColumnName("reason");

            entity.HasOne(d => d.Appointment).WithMany(p => p.DepositForfeitures)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("deposit_forfeitures_appointment_id_fkey");

            entity.HasOne(d => d.Payment).WithOne(p => p.DepositForfeiture)
                .HasForeignKey<DepositForfeiture>(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("deposit_forfeitures_payment_id_fkey");
        });

        modelBuilder.Entity<Diagnosis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("diagnoses_pkey");

            entity.ToTable("diagnoses", "vetcare");

            entity.HasIndex(e => e.ConsultationId, "uq_primary_diagnosis")
                .IsUnique()
                .HasFilter("is_primary");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ConsultationId).HasColumnName("consultation_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");

            entity.HasOne(d => d.Consultation).WithOne(p => p.Diagnosis)
                .HasForeignKey<Diagnosis>(d => d.ConsultationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("diagnoses_consultation_id_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employees_pkey");

            entity.ToTable("employees", "vetcare");

            entity.HasIndex(e => e.DocumentNumber, "employees_document_number_key").IsUnique();

            entity.HasIndex(e => e.LicenseNumber, "employees_license_number_key").IsUnique();

            entity.HasIndex(e => e.UserId, "employees_user_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentNumber)
                .HasMaxLength(20)
                .HasColumnName("document_number");
            entity.Property(e => e.FirstNames)
                .HasMaxLength(120)
                .HasColumnName("first_names");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastNames)
                .HasMaxLength(120)
                .HasColumnName("last_names");
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(50)
                .HasColumnName("license_number");
            entity.Property(e => e.MonthlySalary)
                .HasPrecision(12, 2)
                .HasColumnName("monthly_salary");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.Specialty)
                .HasMaxLength(120)
                .HasColumnName("specialty");
            entity.Property(e => e.Type)
                .HasMaxLength(25)
                .HasDefaultValueSql("'OTHER'::character varying")
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Employee)
                .HasForeignKey<Employee>(d => d.UserId)
                .HasConstraintName("employees_user_id_fkey");
        });

        modelBuilder.Entity<EmployeeSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employee_schedules_pkey");

            entity.ToTable("employee_schedules", "vetcare");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.StartAt).HasColumnName("start_at");
            entity.Property(e => e.Weekday).HasColumnName("weekday");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeSchedules)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employee_schedules_employee_id_fkey");
        });

        modelBuilder.Entity<EmployeeService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employee_services_pkey");

            entity.ToTable("employee_services", "vetcare");

            entity.HasIndex(e => new { e.EmployeeId, e.ServiceId }, "employee_services_employee_id_service_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("is_enabled");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeServices)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employee_services_employee_id_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.EmployeeServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employee_services_service_id_fkey");
        });

        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("inventory_movements_pkey");

            entity.ToTable("inventory_movements", "vetcare");

            entity.HasIndex(e => new { e.BatchId, e.Date }, "ix_inventory_movements_batch");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.BatchId).HasColumnName("batch_id");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.Quantity)
                .HasPrecision(14, 3)
                .HasColumnName("quantity");
            entity.Property(e => e.Reference)
                .HasMaxLength(120)
                .HasColumnName("reference");
            entity.Property(e => e.SaleItemId).HasColumnName("sale_item_id");
            entity.Property(e => e.Type)
                .HasMaxLength(12)
                .HasColumnName("type");
            entity.Property(e => e.UnitCost)
                .HasPrecision(12, 4)
                .HasColumnName("unit_cost");
            entity.Property(e => e.VisitServiceId).HasColumnName("visit_service_id");

            entity.HasOne(d => d.Batch).WithMany(p => p.InventoryMovements)
                .HasForeignKey(d => d.BatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_movements_batch_id_fkey");

            entity.HasOne(d => d.SaleItem).WithMany(p => p.InventoryMovements)
                .HasForeignKey(d => d.SaleItemId)
                .HasConstraintName("fk_inventory_sale_item");

            entity.HasOne(d => d.VisitService).WithMany(p => p.InventoryMovements)
                .HasForeignKey(d => d.VisitServiceId)
                .HasConstraintName("inventory_movements_visit_service_id_fkey");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoices_pkey");

            entity.ToTable("invoices", "vetcare");

            entity.HasIndex(e => new { e.CompanyId, e.Type, e.Series, e.SequenceNumber }, "invoices_company_id_type_series_sequence_number_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CdrRef).HasColumnName("cdr_ref");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CustomerDocNumber)
                .HasMaxLength(20)
                .HasColumnName("customer_doc_number");
            entity.Property(e => e.CustomerDocType)
                .HasMaxLength(10)
                .HasColumnName("customer_doc_type");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customer_name");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.Igv)
                .HasPrecision(12, 2)
                .HasColumnName("igv");
            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.SequenceNumber).HasColumnName("sequence_number");
            entity.Property(e => e.Series)
                .HasMaxLength(8)
                .HasColumnName("series");
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Subtotal)
                .HasPrecision(12, 2)
                .HasColumnName("subtotal");
            entity.Property(e => e.SunatCode)
                .HasMaxLength(30)
                .HasColumnName("sunat_code");
            entity.Property(e => e.SunatMessage).HasColumnName("sunat_message");
            entity.Property(e => e.Total)
                .HasPrecision(12, 2)
                .HasColumnName("total");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .HasColumnName("type");
            entity.Property(e => e.XmlRef).HasColumnName("xml_ref");

            entity.HasOne(d => d.Company).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invoices_company_id_fkey");

            entity.HasOne(d => d.Sale).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.SaleId)
                .HasConstraintName("invoices_sale_id_fkey");
        });

        modelBuilder.Entity<Owner>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("owners_pkey");

            entity.ToTable("owners", "vetcare");

            entity.HasIndex(e => new { e.DocumentType, e.DocumentNumber }, "owners_document_type_document_number_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentNumber)
                .HasMaxLength(20)
                .HasColumnName("document_number");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(10)
                .HasColumnName("document_type");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .HasColumnName("email");
            entity.Property(e => e.FirstNames)
                .HasMaxLength(150)
                .HasColumnName("first_names");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.ToTable("payments", "vetcare");

            entity.HasIndex(e => e.AppointmentId, "ix_payments_appointment");

            entity.HasIndex(e => e.SaleId, "ix_payments_sale");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.CashSessionId).HasColumnName("cash_session_id");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.Method)
                .HasMaxLength(20)
                .HasColumnName("method");
            entity.Property(e => e.Reference)
                .HasMaxLength(120)
                .HasColumnName("reference");
            entity.Property(e => e.RescheduleId).HasColumnName("reschedule_id");
            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.Status)
                .HasMaxLength(12)
                .HasDefaultValueSql("'CONFIRMED'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Payments)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("payments_appointment_id_fkey");

            entity.HasOne(d => d.CashSession).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CashSessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payments_cash_session_id_fkey");

            entity.HasOne(d => d.Reschedule).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RescheduleId)
                .HasConstraintName("payments_reschedule_id_fkey");

            entity.HasOne(d => d.Sale).WithMany(p => p.Payments)
                .HasForeignKey(d => d.SaleId)
                .HasConstraintName("payments_sale_id_fkey");
        });

        modelBuilder.Entity<Pet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pets_pkey");

            entity.ToTable("pets", "vetcare");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.BreedId).HasColumnName("breed_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .HasColumnName("name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Sexo)
                .HasMaxLength(1)
                .HasColumnName("sexo");
            entity.Property(e => e.SpeciesId).HasColumnName("species_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.Pets)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pets_owner_id_fkey");

            entity.HasOne(d => d.Species).WithMany(p => p.Pets)
                .HasForeignKey(d => d.SpeciesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pets_species_id_fkey");

            entity.HasOne(d => d.Breed).WithMany(p => p.Pets)
                .HasPrincipalKey(p => new { p.Id, p.SpeciesId })
                .HasForeignKey(d => new { d.BreedId, d.SpeciesId })
                .HasConstraintName("pets_breed_id_species_id_fkey");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("products_pkey");

            entity.ToTable("products", "vetcare");

            entity.HasIndex(e => e.Sku, "products_sku_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(180)
                .HasColumnName("name");
            entity.Property(e => e.SalePrice)
                .HasPrecision(12, 2)
                .HasColumnName("sale_price");
            entity.Property(e => e.Sku)
                .HasMaxLength(60)
                .HasColumnName("sku");
            entity.Property(e => e.TrackBatches).HasColumnName("track_batches");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasDefaultValueSql("'PRODUCT'::character varying")
                .HasColumnName("type");
            entity.Property(e => e.Unit)
                .HasMaxLength(30)
                .HasDefaultValueSql("'UNIT'::character varying")
                .HasColumnName("unit");
        });

        modelBuilder.Entity<ProductBatch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_batches_pkey");

            entity.ToTable("product_batches", "vetcare");

            entity.HasIndex(e => new { e.Id, e.ProductId }, "product_batches_id_product_id_key").IsUnique();

            entity.HasIndex(e => new { e.ProductId, e.BatchCode }, "product_batches_product_id_batch_code_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.BatchCode)
                .HasMaxLength(100)
                .HasColumnName("batch_code");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UnitCost)
                .HasPrecision(12, 4)
                .HasColumnName("unit_cost");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductBatches)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_batches_product_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles", "vetcare");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(40)
                .HasColumnName("name");
        });

        modelBuilder.Entity<SalaryPayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("salary_payments_pkey");

            entity.ToTable("salary_payments", "vetcare");

            entity.HasIndex(e => new { e.EmployeeId, e.Period }, "salary_payments_employee_id_period_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.Period).HasColumnName("period");
            entity.Property(e => e.Status)
                .HasMaxLength(12)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Employee).WithMany(p => p.SalaryPayments)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("salary_payments_employee_id_fkey");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sales_pkey");

            entity.ToTable("sales", "vetcare");

            entity.HasIndex(e => e.Date, "ix_sales_date");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .HasDefaultValueSql("'OPEN'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Sales)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("sales_appointment_id_fkey");

            entity.HasOne(d => d.Company).WithMany(p => p.Sales)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_company_id_fkey");

            entity.HasOne(d => d.Owner).WithMany(p => p.Sales)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_owner_id_fkey");
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sale_items_pkey");

            entity.ToTable("sale_items", "vetcare");

            entity.HasIndex(e => e.VisitServiceId, "sale_items_visit_service_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.Discount)
                .HasPrecision(12, 2)
                .HasColumnName("discount");
            entity.Property(e => e.Igv)
                .HasPrecision(12, 2)
                .HasColumnName("igv");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasPrecision(12, 3)
                .HasColumnName("quantity");
            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(12, 2)
                .HasColumnName("unit_price");
            entity.Property(e => e.VisitServiceId).HasColumnName("visit_service_id");

            entity.HasOne(d => d.Product).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("sale_items_product_id_fkey");

            entity.HasOne(d => d.Sale).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sale_items_sale_id_fkey");

            entity.HasOne(d => d.VisitService).WithOne(p => p.SaleItem)
                .HasForeignKey<SaleItem>(d => d.VisitServiceId)
                .HasConstraintName("sale_items_visit_service_id_fkey");
        });

        modelBuilder.Entity<ScheduleBlock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("schedule_blocks_pkey");

            entity.ToTable("schedule_blocks", "vetcare");

            entity.HasIndex(e => new { e.EmployeeId, e.StartAt, e.EndAt }, "ix_schedule_blocks_employee");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.StartAt).HasColumnName("start_at");

            entity.HasOne(d => d.Employee).WithMany(p => p.ScheduleBlocks)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("schedule_blocks_employee_id_fkey");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("services_pkey");

            entity.ToTable("services", "vetcare");

            entity.HasIndex(e => e.Name, "services_name_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.BasePrice)
                .HasPrecision(12, 2)
                .HasColumnName("base_price");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsClinical).HasColumnName("is_clinical");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
        });

        modelBuilder.Entity<ServiceSupply>(entity =>
        {
            entity.HasKey(e => new { e.ServiceId, e.ProductId }).HasName("service_supplies_pkey");

            entity.ToTable("service_supplies", "vetcare");

            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasPrecision(14, 3)
                .HasColumnName("quantity");

            entity.HasOne(d => d.Product).WithMany(p => p.ServiceSupplies)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("service_supplies_product_id_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceSupplies)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("service_supplies_service_id_fkey");
        });

        modelBuilder.Entity<SettlementItem>(entity =>
        {
            entity.HasKey(e => new { e.SettlementId, e.CommissionId }).HasName("settlement_items_pkey");

            entity.ToTable("settlement_items", "vetcare");

            entity.HasIndex(e => e.CommissionId, "settlement_items_commission_id_key").IsUnique();

            entity.Property(e => e.SettlementId).HasColumnName("settlement_id");
            entity.Property(e => e.CommissionId).HasColumnName("commission_id");

            entity.HasOne(d => d.Commission).WithOne(p => p.SettlementItem)
                .HasForeignKey<SettlementItem>(d => d.CommissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("settlement_items_commission_id_fkey");

            entity.HasOne(d => d.Settlement).WithMany(p => p.SettlementItems)
                .HasForeignKey(d => d.SettlementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("settlement_items_settlement_id_fkey");
        });

        modelBuilder.Entity<Species>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("species_pkey");

            entity.ToTable("species", "vetcare");

            entity.HasIndex(e => e.Name, "species_name_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(80)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Treatment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("treatments_pkey");

            entity.ToTable("treatments", "vetcare");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ConsultationId).HasColumnName("consultation_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Dosage).HasColumnName("dosage");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.Frequency).HasColumnName("frequency");
            entity.Property(e => e.Instructions).HasColumnName("instructions");

            entity.HasOne(d => d.Consultation).WithMany(p => p.Treatments)
                .HasForeignKey(d => d.ConsultationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("treatments_consultation_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users", "vetcare");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("users_role_id_fkey");
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("visits_pkey");

            entity.ToTable("visits", "vetcare");

            entity.HasIndex(e => new { e.PetId, e.StartAt }, "ix_visits_pet");

            entity.HasIndex(e => e.AppointmentId, "visits_appointment_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PetId).HasColumnName("pet_id");
            entity.Property(e => e.StartAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("start_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'IN_PROGRESS'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Appointment).WithOne(p => p.Visit)
                .HasForeignKey<Visit>(d => d.AppointmentId)
                .HasConstraintName("visits_appointment_id_fkey");

            entity.HasOne(d => d.Pet).WithMany(p => p.Visits)
                .HasForeignKey(d => d.PetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("visits_pet_id_fkey");
        });

        modelBuilder.Entity<VisitService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("visit_services_pkey");

            entity.ToTable("visit_services", "vetcare");

            entity.HasIndex(e => new { e.Id, e.ServiceId }, "visit_services_id_service_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.FinalPrice)
                .HasPrecision(12, 2)
                .HasColumnName("final_price");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.VisitId).HasColumnName("visit_id");

            entity.HasOne(d => d.Employee).WithMany(p => p.VisitServices)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("visit_services_employee_id_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.VisitServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("visit_services_service_id_fkey");

            entity.HasOne(d => d.Visit).WithMany(p => p.VisitServices)
                .HasForeignKey(d => d.VisitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("visit_services_visit_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
