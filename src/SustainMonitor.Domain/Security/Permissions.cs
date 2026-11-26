namespace SustainMonitor.Domain.Security;

/// <summary>
/// Who may do what inside a company. Platform admins act outside companies
/// (verify companies, approve e-certs) and are checked by Identity role instead.
/// </summary>
public static class Permissions
{
    public static bool CanManageTeam(CompanyRole r) => r == CompanyRole.CEO;
    public static bool CanCreateProject(CompanyRole r) => r == CompanyRole.ProjectManager;
    public static bool CanSubmitBaseline(CompanyRole r) => r == CompanyRole.ProjectManager;
    public static bool CanCreateTicket(CompanyRole r) => r == CompanyRole.ProjectManager;
    public static bool CanUploadEvidence(CompanyRole r) => r == CompanyRole.ProjectManager;
    public static bool CanReviewTicket(CompanyRole r) => r == CompanyRole.Superior;
    public static bool CanRunAnalysis(CompanyRole r) => r is CompanyRole.CEO or CompanyRole.ProjectManager;
    public static bool CanDownloadCertificate(CompanyRole r) => r is CompanyRole.CEO or CompanyRole.ProjectManager;
    public static bool CanView(CompanyRole r) => true;
}
