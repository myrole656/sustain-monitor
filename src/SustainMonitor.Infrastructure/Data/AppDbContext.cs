using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Entities;

namespace SustainMonitor.Infrastructure.Data;

public class AppUser : IdentityUser
{
    public string? FullName { get; set; }
    /// <summary>Set for invited users with a temporary password; cleared when they change it.</summary>
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    // GBI catalogue
    public DbSet<GbiTool> GbiTools => Set<GbiTool>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Criterion> Criteria => Set<Criterion>();
    public DbSet<CriterionVariant> CriterionVariants => Set<CriterionVariant>();
    public DbSet<CriterionLevel> CriterionLevels => Set<CriterionLevel>();
    public DbSet<EvidenceRequirement> EvidenceRequirements => Set<EvidenceRequirement>();
    public DbSet<RatingBand> RatingBands => Set<RatingBand>();
    public DbSet<SdgMapping> SdgMappings => Set<SdgMapping>();

    // Companies
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
    public DbSet<ECertificate> ECertificates => Set<ECertificate>();

    // Projects and assessments
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<CriterionScore> CriterionScores => Set<CriterionScore>();
    public DbSet<CategoryScore> CategoryScores => Set<CategoryScore>();
    public DbSet<Evidence> EvidenceFiles => Set<Evidence>();

    // CO-tickets
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();

    // AI and system
    public DbSet<AiAnalysis> AiAnalyses => Set<AiAnalysis>();
    public DbSet<Notification> AppNotifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // Enums as readable text, so the .db file makes sense in DB Browser for SQLite.
        b.Properties<AssessmentStage>().HaveConversion<string>().HaveMaxLength(32);
        b.Properties<AssessmentTrigger>().HaveConversion<string>().HaveMaxLength(32);
        b.Properties<CriterionStatus>().HaveConversion<string>().HaveMaxLength(32);
        b.Properties<TicketType>().HaveConversion<string>().HaveMaxLength(32);
        b.Properties<TicketStatus>().HaveConversion<string>().HaveMaxLength(32);
        b.Properties<TicketPriority>().HaveConversion<string>().HaveMaxLength(32);
        b.Properties<CompanyRole>().HaveConversion<string>().HaveMaxLength(32);
        b.Properties<CompanyStatus>().HaveConversion<string>().HaveMaxLength(32);
        b.Properties<ECertStatus>().HaveConversion<string>().HaveMaxLength(32);
        b.Properties<AiAnalysisKind>().HaveConversion<string>().HaveMaxLength(32);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        // ---- Catalogue ----
        m.Entity<GbiTool>(e =>
        {
            e.HasIndex(x => new { x.Code, x.Version }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(16);
        });
        m.Entity<Category>(e =>
        {
            e.HasIndex(x => new { x.ToolId, x.Code }).IsUnique();
            e.HasOne(x => x.Tool).WithMany(t => t.Categories).HasForeignKey(x => x.ToolId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<Criterion>(e =>
        {
            e.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
            e.HasOne(x => x.Category).WithMany(c => c.Criteria).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Criteria_MaxPoints", "\"MaxPoints\" >= 0"));
        });
        m.Entity<CriterionVariant>().HasOne(x => x.Criterion).WithMany(c => c.Variants).HasForeignKey(x => x.CriterionId);
        m.Entity<CriterionLevel>().HasOne(x => x.Criterion).WithMany(c => c.Levels).HasForeignKey(x => x.CriterionId);
        m.Entity<EvidenceRequirement>().HasOne(x => x.Criterion).WithMany(c => c.EvidenceRequirements).HasForeignKey(x => x.CriterionId);
        m.Entity<RatingBand>().HasOne(x => x.Tool).WithMany(t => t.RatingBands).HasForeignKey(x => x.ToolId);
        m.Entity<SdgMapping>(e =>
        {
            e.HasKey(x => new { x.CriterionId, x.SdgNumber });
            e.HasOne(x => x.Criterion).WithMany(c => c.SdgMappings).HasForeignKey(x => x.CriterionId);
        });

        // ---- Companies ----
        m.Entity<Company>(e =>
        {
            e.HasIndex(x => x.SsmNo).IsUnique();
            e.HasIndex(x => x.Status);
            e.Property(x => x.SsmNo).HasMaxLength(20);
            e.Property(x => x.CidbGrade).HasMaxLength(4);
        });
        m.Entity<CompanyUser>(e =>
        {
            e.HasKey(x => new { x.CompanyId, x.UserId });
            e.HasIndex(x => x.UserId).IsUnique(); // one company per person
            e.HasOne(x => x.Company).WithMany(c => c.Users).HasForeignKey(x => x.CompanyId);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId);
        });
        m.Entity<ECertificate>(e =>
        {
            e.HasIndex(x => x.CertNo).IsUnique().HasFilter("\"CertNo\" IS NOT NULL");
            e.HasIndex(x => x.VerifyToken).IsUnique().HasFilter("\"VerifyToken\" IS NOT NULL");
            e.HasIndex(x => new { x.ProjectId, x.Status });
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.Assessment).WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Projects ----
        m.Entity<Project>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(10);
            e.HasOne(x => x.Company).WithMany(c => c.Projects).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tool).WithMany().HasForeignKey(x => x.ToolId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Assessments (immutable snapshots) ----
        m.Entity<Assessment>(e =>
        {
            e.HasOne(x => x.Project).WithMany(p => p.Assessments).HasForeignKey(x => x.ProjectId);
            e.HasIndex(x => new { x.ProjectId, x.VersionNo }).IsUnique();
            // Only one current snapshot per project (SQLite partial index).
            e.HasIndex(x => x.ProjectId).IsUnique().HasFilter("\"IsCurrent\" = 1")
                .HasDatabaseName("IX_Assessments_ProjectId_Current");
        });
        m.Entity<CriterionScore>(e =>
        {
            e.HasKey(x => new { x.AssessmentId, x.CriterionId });
            e.HasOne(x => x.Assessment).WithMany(a => a.CriterionScores).HasForeignKey(x => x.AssessmentId);
            e.HasOne(x => x.Criterion).WithMany().HasForeignKey(x => x.CriterionId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_CriterionScores_Points", "\"PointsAwarded\" >= 0 AND \"PointsTargeted\" >= 0"));
        });
        m.Entity<CategoryScore>(e =>
        {
            e.HasKey(x => new { x.AssessmentId, x.CategoryId });
            e.HasOne(x => x.Assessment).WithMany(a => a.CategoryScores).HasForeignKey(x => x.AssessmentId);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<Evidence>(e =>
        {
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.Criterion).WithMany().HasForeignKey(x => x.CriterionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Ticket).WithMany(t => t.Evidence).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---- CO-tickets ----
        m.Entity<Ticket>(e =>
        {
            e.HasIndex(x => x.Key).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.Status });
            e.Property(x => x.Key).HasMaxLength(24);
            e.HasOne(x => x.Project).WithMany(p => p.Tickets).HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.Criterion).WithMany().HasForeignKey(x => x.CriterionId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<TicketComment>().HasOne(x => x.Ticket).WithMany(t => t.Comments).HasForeignKey(x => x.TicketId);
        m.Entity<TicketHistory>().HasOne(x => x.Ticket).WithMany(t => t.History).HasForeignKey(x => x.TicketId);

        // ---- AI and system ----
        m.Entity<AiAnalysis>().HasOne(x => x.Assessment).WithMany().HasForeignKey(x => x.AssessmentId);
        m.Entity<Notification>(e =>
        {
            e.ToTable("AppNotifications");
            e.HasIndex(x => new { x.UserId, x.ReadAt });
        });
        m.Entity<AuditLog>().HasIndex(x => x.At);
    }
}
