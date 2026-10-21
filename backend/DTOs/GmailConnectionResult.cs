namespace backend.DTOs;

public enum GmailConnectResult
{
    Success,
    InvalidState,
    GoogleError,
    NoRefreshToken
}