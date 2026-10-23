namespace backend.Models;

public enum EmailProcessingStatus
{
    Pending = 0,        // saved, not judged yet
    NotJobRelated = 1,  // classifier said no; final
    Staged = 2,          // job related, written to pending tables; final
    NoAction = 3,
    Skipped = 4
}