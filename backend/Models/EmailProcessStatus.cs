namespace backend.Models;

public enum EmailProcessingStatus
{
    Pending = 0,    
    NotJobRelated = 1,
    Staged = 2,
    NoAction = 3,
    Skipped = 4
}