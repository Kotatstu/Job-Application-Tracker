public enum ReviewResult
{
    Success,
    NotFound,            // pending item missing or not the users
    AlreadyReviewed,
    CompanyNotFound,
    ApplicationNotFound,
    InvalidStatus
}