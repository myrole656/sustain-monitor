using System.Text.Json;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Scoring;

namespace SustainMonitor.Web.Models;

// ---- Auth ----
public record TokenResponse(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);
public record MembershipDto(int CompanyId, string Company, CompanyStatus CompanyStatus, CompanyRole Role, bool IsActive);
public record MeDto(string Id, string Email, string? FullName, bool MustChangePassword, List<string> Roles, MembershipDto? Membership);

// ---- Company ----
public record TeamMemberDto(string UserId, string FullName, CompanyRole Role, bool IsActive, DateTime JoinedAt);
public record CompanyDto(int Id, string Name, string SsmNo, string CidbGrade, string? Address, string? Phone,
    CompanyStatus Status, string? StatusNote, CompanyRole MyRole, List<TeamMemberDto> Team);
public record RegisterCompanyResult(int Id, string Name, CompanyStatus Status, string Message);

// ---- Catalogue ----
public record ToolSummary(int Id, string Code, string Name, string Version);
public record VariantDto(string ConditionKey, string Description, int MaxPoints);
public record CriterionDto(int Id, string Code, string Name, int MaxPoints, string? ReferenceUrl, List<VariantDto> Variants);
public record CategoryDto(int Id, string Code, string Name, int MaxPoints, List<CriterionDto> Criteria);
public record RatingBandDto(string Name, int MinPoints, int MaxPoints);
public record ToolDetail(int Id, string Code, string Name, string Version, List<RatingBandDto> RatingBands, List<CategoryDto> Categories);

// ---- Projects ----
public record SnapshotSummary(int VersionNo, int TotalPoints, string Rating, DateTime CreatedAt);
public record ProjectListItem(int Id, string Code, string Name, string? Location, string? TargetRating, string Tool,
    SnapshotSummary? Current, int OpenTickets);
public record ToolInfo(string Code, string Name, string Version);
public record CategoryScoreDto(string Code, string Name, int Points, int MaxPoints);
public record CriterionScoreDto(int CriterionId, string Code, string Name, string Category, CriterionStatus Status,
    int PointsTargeted, int PointsAwarded, int Max, int OpenStake);
public record CurrentDetail(int Id, int VersionNo, int TotalPoints, string Rating, DateTime CreatedAt, AssessmentTrigger Trigger,
    List<CategoryScoreDto> Categories, List<CriterionScoreDto> Criteria);
public record ProjectDetail(int Id, string Code, string Name, string? Location, string? BuildingType, decimal? GfaM2,
    string? TargetRating, int ToolId, ToolInfo Tool, CompanyRole MyRole, ProgressReport? Progress, CurrentDetail? Current);
public record HistoryItem(int VersionNo, int TotalPoints, string Rating, AssessmentTrigger Trigger, int? TriggerTicketId, DateTime CreatedAt);
public record CreatedRef(int Id, string? Code, string? Name, string? Key);
public record SuggestedTicket(int CriterionId, string CriterionCode, string CriterionName, string CategoryCode, int PointsAtStake);
public record BaselineResult(int Id, int TotalPoints, string Rating, List<SuggestedTicket> SuggestedTickets);
public record CertificateOutcome(string Status, string Message);
public record AnalysisResult(int TotalPoints, int MaxPoints, string Rating, List<CategoryResult> Categories,
    ProgressReport Progress, CertificateOutcome Certificate);

// ---- Tickets ----
public record BoardCard(int Id, string Key, string Title, TicketPriority Priority, TicketType Type, int PointsAtStake,
    int? PointsAwardedOnClose, DateOnly? DueDate, string Criterion, string Category, int Evidence, List<TicketStatus> AllowedMoves);
public record BoardColumn(TicketStatus Status, List<BoardCard> Tickets);
public record Board(CompanyRole MyRole, List<BoardColumn> Columns);
public record TicketCriterion(string Code, string Name, int MaxPoints, string? ReferenceUrl);
public record EvidenceDto(int Id, string FileName, AssessmentStage Stage, DateTime UploadedAt, string UploadedBy);
public record CommentDto(string Author, string Body, DateTime CreatedAt);
public record HistoryDto(string Field, string? OldValue, string? NewValue, string ChangedBy, DateTime ChangedAt);
public record TicketDetail(int Id, int ProjectId, string Key, string Title, string? Description, TicketStatus Status,
    TicketPriority Priority, TicketType Type, int PointsAtStake, int? PointsAwardedOnClose, DateOnly? DueDate,
    DateTime CreatedAt, DateTime? ClosedAt, string Reporter, string Reviewer, TicketCriterion Criterion,
    List<TicketStatus> AllowedMoves, List<EvidenceDto> Evidence, List<CommentDto> Comments, List<HistoryDto> History);
public record ScoreSide(int VersionNo, int TotalPoints, string Rating);
public record ScoreChange(ScoreSide Before, ScoreSide After, bool RatingChanged);
public record MoveResult(int Id, string Key, TicketStatus Status, int? PointsAwardedOnClose, ScoreChange? Score);

// ---- E-certs ----
public record ECertListItem(int Id, string? CertNo, ECertStatus Status, int Points, string Rating, string ToolCode, string ToolVersion,
    DateTime RequestedAt, DateTime? IssuedAt, DateTime? ExpiresAt, string? DecisionNote, string? VerifyUrl);
public record ECertPrint(int Id, string? CertNo, ECertStatus Status, string PublicStatus, string Company, string SsmNo, string CidbGrade,
    string Project, string ProjectCode, string? Location, string ToolCode, string ToolVersion, int Points, string Rating,
    DateTime? IssuedAt, DateTime? ExpiresAt, string? VerifyToken, List<CategoryScoreDto> Categories, string Disclaimer);
public record VerifyResult(string Status, string? CertNo, string? Company, string? SsmNo, string? Project, string? Tool,
    int Points, string? Rating, DateTime? IssuedAt, DateTime? ExpiresAt, string? Disclaimer);

// ---- Notifications ----
public record NotificationDto(int Id, string Type, DateTime CreatedAt, DateTime? ReadAt, JsonElement Content)
{
    public string Subject => Content.TryGetProperty("subject", out var s) ? s.GetString() ?? "" : Type;
    public string Message => Content.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
}

// ---- Admin ----
public record AdminCompany(int Id, string Name, string SsmNo, string CidbGrade, string? Address, string? Phone,
    CompanyStatus Status, string? StatusNote, DateTime CreatedAt, string? Ceo);
public record AdminECert(int Id, string? CertNo, ECertStatus Status, int Points, string Rating, string ToolCode, string ToolVersion,
    DateTime RequestedAt, string? DecisionNote, string Project, string ProjectCode, string Company, int AssessmentId);
public record AdminMembership(string UserId, CompanyRole Role, bool IsActive, string Company);
public record AdminUser(string Id, string Email, string? FullName, bool MustChangePassword, DateTime CreatedAt, bool Locked, AdminMembership? Membership);
