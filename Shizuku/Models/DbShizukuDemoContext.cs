using Microsoft.EntityFrameworkCore;
using Shizuku.Models.System;
using System;
using System.Collections.Generic;

namespace Shizuku.Models;

public partial class DbShizukuDemoContext : DbContext
{
    public DbShizukuDemoContext()
    {
    }

    public DbShizukuDemoContext(DbContextOptions<DbShizukuDemoContext> options)
        : base(options)
    {
    }
    public DbSet<SystemLog> SystemLogs { get; set; }
    public virtual DbSet<TAttendanceRecord> TAttendanceRecords { get; set; }

    public virtual DbSet<TDepartment> TDepartments { get; set; }

    public virtual DbSet<TEmployee> TEmployees { get; set; }

    public virtual DbSet<TLeaveRecord> TLeaveRecords { get; set; }

    public virtual DbSet<TMember> TMembers { get; set; }

    public virtual DbSet<TMemberVerification> TMemberVerifications { get; set; }

    public virtual DbSet<TPaymentLog> TPaymentLogs { get; set; }

    public virtual DbSet<TPaymentMethod> TPaymentMethods { get; set; }

    public virtual DbSet<TPaymentTransaction> TPaymentTransactions { get; set; }

    public virtual DbSet<TPosition> TPositions { get; set; }

    public virtual DbSet<TProduct> TProducts { get; set; }

    public virtual DbSet<TProductCategory> TProductCategories { get; set; }

    public virtual DbSet<TProductColor> TProductColors { get; set; }

    public virtual DbSet<TProductImage> TProductImages { get; set; }

    public virtual DbSet<TProductSize> TProductSizes { get; set; }

    public virtual DbSet<TProductVariant> TProductVariants { get; set; }

    public virtual DbSet<TRefund> TRefunds { get; set; }

    public virtual DbSet<TTicketCategory> TTicketCategories { get; set; }

    public virtual DbSet<TTicketMessage> TTicketMessages { get; set; }

    public virtual DbSet<TTicketsCustomer> TTicketsCustomers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=dbShizukuDemo;Integrated Security=True;Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.Property(e => e.Level).HasMaxLength(16);
            entity.Property(e => e.Timestamp).HasColumnType("datetime");
        });

        modelBuilder.Entity<TAttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_Attendance_records");

            entity.ToTable("tAttendanceRecords");

            entity.Property(e => e.FId)
                .ValueGeneratedOnAdd()
                .HasColumnName("fId");
            entity.Property(e => e.FClockInTime)
                .HasColumnType("datetime")
                .HasColumnName("fClock_in_time");
            entity.Property(e => e.FClockOutTime)
                .HasColumnType("datetime")
                .HasColumnName("fClock_out_time");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())", "DF_Attendance_records_fCreated_at")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_at");
            entity.Property(e => e.FEmployeeId).HasColumnName("fEmployee_id");
            entity.Property(e => e.FStatus)
                .HasMaxLength(50)
                .HasColumnName("fStatus");
            entity.Property(e => e.FWorkDate).HasColumnName("fWork_date");
        });

        modelBuilder.Entity<TDepartment>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_Departments");

            entity.ToTable("tDepartments");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FDepartmentName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("fDepartment_name");
            entity.Property(e => e.FManagerId).HasColumnName("fManager_id");
        });

        modelBuilder.Entity<TEmployee>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_Employees");

            entity.ToTable("tEmployees");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FAddress)
                .HasMaxLength(200)
                .HasColumnName("fAddress");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())", "DF_Employees_fCreated_at")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_at");
            entity.Property(e => e.FDepartmentId).HasColumnName("fDepartment_id");
            entity.Property(e => e.FEmail)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fEmail");
            entity.Property(e => e.FHireDate).HasColumnName("fHire_date");
            entity.Property(e => e.FName)
                .HasMaxLength(50)
                .HasColumnName("fName");
            entity.Property(e => e.FNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fNumber");
            entity.Property(e => e.FPassword)
                .HasMaxLength(50)
                .HasColumnName("fPassword");
            entity.Property(e => e.FPhone)
                .HasMaxLength(50)
                .HasColumnName("fPhone");
            entity.Property(e => e.FPositionId).HasColumnName("fPosition_id");
            entity.Property(e => e.FStatus)
                .HasMaxLength(50)
                .HasDefaultValue("active", "DF_Employees_fStatus")
                .HasColumnName("fStatus");
            entity.Property(e => e.FUpdatedAt)
                .HasDefaultValueSql("(getdate())", "DF_Employees_fUpdated_at")
                .HasColumnType("datetime")
                .HasColumnName("fUpdated_at");
        });

        modelBuilder.Entity<TLeaveRecord>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_Leave_records");

            entity.ToTable("tLeaveRecords");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())", "DF_Leave_records_fCreated_at")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_at");
            entity.Property(e => e.FEmployeeId).HasColumnName("fEmployee_id");
            entity.Property(e => e.FEndDate)
                .HasColumnType("datetime")
                .HasColumnName("fEnd_date");
            entity.Property(e => e.FLeaveType).HasDefaultValue(1).HasColumnName("fLeave_type");
            entity.Property(e => e.FStartDate)
                .HasColumnType("datetime")
                .HasColumnName("fStart_date");
            entity.Property(e => e.FStatus).HasColumnName("fStatus");
        });

        modelBuilder.Entity<TMember>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK__tMember__D9F8227C49D3B076");

            entity.ToTable("tMember");

            entity.HasIndex(e => e.FAccount, "UQ__tMember__E1299463029448AA").IsUnique();

            entity.HasIndex(e => e.FAccount, "UQ__tMember__E12994638CC4844E").IsUnique();

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FAccount)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fAccount");
            entity.Property(e => e.FBirthday).HasColumnName("fBirthday");
            entity.Property(e => e.FCreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_time");
            entity.Property(e => e.FEmail)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("fEmail");
            entity.Property(e => e.FGender)
                .HasDefaultValue(0)
                .HasColumnName("fGender");
            entity.Property(e => e.FImage)
                .HasMaxLength(255)
                .HasColumnName("fImage");
            entity.Property(e => e.FIpAddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("fIpAddress");
            entity.Property(e => e.FIsActive)
                .HasDefaultValue(true)
                .HasColumnName("fIs_active");
            entity.Property(e => e.FLevel)
                .HasDefaultValue(1)
                .HasColumnName("fLevel");
            entity.Property(e => e.FLoginTime)
                .HasColumnType("datetime")
                .HasColumnName("fLoginTime");
            entity.Property(e => e.FMemberId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fMemberId");
            entity.Property(e => e.FName)
                .HasMaxLength(50)
                .HasColumnName("fName");
            entity.Property(e => e.FPassword)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("fPassword");
            entity.Property(e => e.FPhone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("fPhone");
            entity.Property(e => e.FReceiverAddress)
                .HasMaxLength(255)
                .HasColumnName("fReceiver_address");
            entity.Property(e => e.FReceiverName)
                .HasMaxLength(50)
                .HasColumnName("fReceiver_name");
            entity.Property(e => e.FReceiverPhone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("fReceiver_phone");
            entity.Property(e => e.FUpdatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fUpdated_time");
            entity.Property(e => e.FWishlist).HasColumnName("fWishlist");
        });

        modelBuilder.Entity<TMemberVerification>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK__tMemberV__D9F8227C9F10AE3B");

            entity.ToTable("tMemberVerification");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FAttemptCount)
                .HasDefaultValue(0)
                .HasColumnName("fAttempt_count");
            entity.Property(e => e.FCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("fCode");
            entity.Property(e => e.FCreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_time");
            entity.Property(e => e.FExpireTime)
                .HasColumnType("datetime")
                .HasColumnName("fExpire_time");
            entity.Property(e => e.FIsUsed)
                .HasDefaultValue(false)
                .HasColumnName("fIs_used");
            entity.Property(e => e.FMemberId).HasColumnName("fMember_id");
            entity.Property(e => e.FType).HasColumnName("fType");
        });

        modelBuilder.Entity<TPaymentLog>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_Payment_logs");

            entity.ToTable("tPaymentLogs");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FActionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fAction_type");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())", "DF_Payment_logs_fCreated_at")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_at");
            entity.Property(e => e.FPaymentTransactionsId).HasColumnName("fPayment_transactions_id");
            entity.Property(e => e.FRequestData).HasColumnName("fRequest_data");
            entity.Property(e => e.FResponseData).HasColumnName("fResponse_data");
        });

        modelBuilder.Entity<TPaymentMethod>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_Payment_methods");

            entity.ToTable("tPaymentMethods");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FHandlingFee)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("fHandling_fee");
            entity.Property(e => e.FIsActive)
                .HasDefaultValue(true, "DF_Payment_methods_fIs_active")
                .HasColumnName("fIs_active");
            entity.Property(e => e.FMethodName)
                .HasMaxLength(200)
                .HasColumnName("fMethod_name");
            entity.Property(e => e.FProviderCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fProvider_code");
        });

        modelBuilder.Entity<TPaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_Payment_transactions");

            entity.ToTable("tPaymentTransactions");
            entity.Property(e => e.FId)
                .ValueGeneratedOnAdd()
                .HasColumnName("fId");
            entity.Property(e => e.FAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("fAmount");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())", "DF_Payment_transactions_fCreated_at")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_at");
            entity.Property(e => e.FGatewayTradeNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fGateway_trade_no");
            entity.Property(e => e.FMemberId).HasColumnName("fMember_id");
            entity.Property(e => e.FMethodId).HasColumnName("fMethod_id");
            entity.Property(e => e.FOrderId).HasColumnName("fOrder_id");
            entity.Property(e => e.FPaidAt)
                .HasColumnType("datetime")
                .HasColumnName("fPaid_at");
            entity.Property(e => e.FStatus).HasColumnName("fStatus");
            entity.Property(e => e.FTransactionNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fTransaction_no");
        });

        modelBuilder.Entity<TPosition>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_Positions");

            entity.ToTable("tPositions");

            entity.Property(e => e.FId).HasColumnName("fID");
            entity.Property(e => e.FLevel).HasColumnName("fLevel");
            entity.Property(e => e.FPositionName)
                .HasMaxLength(200)
                .HasColumnName("fPosition_name");
        });

        modelBuilder.Entity<TProduct>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK__tProduct__D9F8227CA9D0A366");

            entity.ToTable("tProduct");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FCategoryId).HasColumnName("fCategoryId");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fCreatedAt");
            entity.Property(e => e.FDescription).HasColumnName("fDescription");
            entity.Property(e => e.FName)
                .HasMaxLength(255)
                .HasColumnName("fName");
            entity.Property(e => e.FPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("fPrice");
            entity.Property(e => e.FProduct)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fProduct");
            entity.Property(e => e.FStatus)
                .HasDefaultValue((byte)1)
                .HasComment("0: 刪除 1:上架 2:下架")
                .HasColumnName("fStatus");
        });

        modelBuilder.Entity<TProductCategory>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK__tProduct__D9F8227C43D194BC");

            entity.ToTable("tProductCategories");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FCodePrefix)
                .HasMaxLength(10)
                .HasColumnName("fCodePrefix");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fCreatedAt");
            entity.Property(e => e.FName)
                .HasMaxLength(100)
                .HasColumnName("fName");
            entity.Property(e => e.FParentId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fParentId");
        });

        modelBuilder.Entity<TProductColor>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK__tProduct__D9F8227C322208B3");

            entity.ToTable("tProductColors");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FColorCode)
                .HasMaxLength(7)
                .HasColumnName("fColorCode");
            entity.Property(e => e.FName)
                .HasMaxLength(50)
                .HasColumnName("fName");
        });

        modelBuilder.Entity<TProductImage>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK__tProduct__D9F8227C1A32E26B");

            entity.ToTable("tProductImages");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FImageUrl)
                .HasMaxLength(500)
                .HasColumnName("fImageUrl");
            entity.Property(e => e.FIsMain).HasColumnName("fIsMain");
            entity.Property(e => e.FProductId).HasColumnName("fProductId");
            entity.Property(e => e.FSortOrder).HasColumnName("fSortOrder");
        });

        modelBuilder.Entity<TProductSize>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK__tProduct__D9F8227C333036AA");

            entity.ToTable("tProductSizes");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FName)
                .HasMaxLength(20)
                .HasColumnName("fName");
            entity.Property(e => e.FSortOrder).HasColumnName("fSortOrder");
        });

        modelBuilder.Entity<TProductVariant>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK__tProduct__D9F8227C49146C6E");

            entity.ToTable("tProductVariants");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FColorId).HasColumnName("fColorId");
            entity.Property(e => e.FPrice)
                .HasColumnType("decimal(6, 2)")
                .HasColumnName("fPrice");
            entity.Property(e => e.FProductId).HasColumnName("fProductId");
            entity.Property(e => e.FSizeId).HasColumnName("fSizeId");
            entity.Property(e => e.FSkuCode)
                .HasMaxLength(100)
                .HasColumnName("fSkuCode");
            entity.Property(e => e.FStock).HasColumnName("fStock");
            // ✨ 加上這段：強制指定外鍵關聯，解決 TProductFId 報錯
            entity.HasOne(d => d.TProduct)
                .WithMany(p => p.TProductVariants)
                .HasForeignKey(d => d.FProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_tProductVariants_tProduct");

        });

        modelBuilder.Entity<TRefund>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_Refunds");

            entity.ToTable("tRefunds");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FMemberId).HasColumnName("fMember_id");
            entity.Property(e => e.FOrderId).HasColumnName("fOrder_id");
            entity.Property(e => e.FProcessedAt)
                .HasDefaultValueSql("(getdate())", "DF_Refunds_fProcessed_at")
                .HasColumnType("datetime")
                .HasColumnName("fProcessed_at");
            entity.Property(e => e.FReason)
                .HasMaxLength(200)
                .HasColumnName("fReason");
            entity.Property(e => e.FRefundAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("fRefund_amount");
            entity.Property(e => e.FRefundNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fRefund_no");
            entity.Property(e => e.FStatus).HasColumnName("fStatus");
            entity.Property(e => e.FTransactionId).HasColumnName("fTransaction_id");
        });

        modelBuilder.Entity<TTicketCategory>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_ticket_categories");

            entity.ToTable("tTicketCategories");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_at");
            entity.Property(e => e.FDescription)
                .HasMaxLength(255)
                .HasColumnName("fDescription");
            entity.Property(e => e.FIsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("fIsDeleted");
            entity.Property(e => e.FName)
                .HasMaxLength(50)
                .HasColumnName("fName");
        });

        modelBuilder.Entity<TTicketMessage>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_ticket_messages");

            entity.ToTable("tTicketMessages");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_at");
            entity.Property(e => e.FIsRead)
                .HasDefaultValue(false)
                .HasColumnName("fIs_read");
            entity.Property(e => e.FMessage).HasColumnName("fMessage");
            entity.Property(e => e.FSenderId).HasColumnName("fSender_id");
            entity.Property(e => e.FTicketId).HasColumnName("fTicket_id");
        });

        modelBuilder.Entity<TTicketsCustomer>(entity =>
        {
            entity.HasKey(e => e.FId).HasName("PK_tickets_customer");

            entity.ToTable("tTicketsCustomer");

            entity.Property(e => e.FId).HasColumnName("fId");
            entity.Property(e => e.FAssignedAgentId).HasColumnName("fAssigned_agent_id");
            entity.Property(e => e.FCategoryId).HasColumnName("fCategory_id");
            entity.Property(e => e.FClosedAt)
                .HasColumnType("datetime")
                .HasColumnName("fClosed_at");
            entity.Property(e => e.FCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fCreated_at");
            entity.Property(e => e.FIsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("fIsDeleted");
            entity.Property(e => e.FMemberId).HasColumnName("fMember_id");
            entity.Property(e => e.FOrderId).HasColumnName("fOrder_id");
            entity.Property(e => e.FPriority)
                .HasMaxLength(10)
                .HasDefaultValue("中")
                .HasColumnName("fPriority");
            entity.Property(e => e.FStatus)
                .HasMaxLength(20)
                .HasDefaultValue("待處理")
                .HasColumnName("fStatus");
            entity.Property(e => e.FSubject)
                .HasMaxLength(100)
                .HasColumnName("fSubject");
            entity.Property(e => e.FUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fUpdated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
