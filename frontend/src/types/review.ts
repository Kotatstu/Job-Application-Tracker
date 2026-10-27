export type ReviewStatus = 'Pending' | 'Confirmed' | 'Dismissed'

export interface EmailContext {
    emailMessageId: number
    subject: string
    senderEmail: string
    receivedAt: string
    snippet: string | null
}

export interface PendingApplication {
    id: number
    companyNameGuess: string
    jobTitleGuess: string | null
    statusGuess: string | null
    reviewStatus: ReviewStatus
    detectedAt: string
    reviewedAt: string | null
    linkedJobApplicationId: number | null
    email: EmailContext
}

export interface SuggestedApplication {
    id: number
    companyName: string
    jobTitle: string
    currentStatus: string
}

export interface PendingStatusChange {
    id: number
    suggestedApplication: SuggestedApplication | null
    statusGuess: string
    noteGuess: string | null
    reviewStatus: ReviewStatus
    detectedAt: string
    reviewedAt: string | null
    email: EmailContext
}

export interface ReviewSummary {
    pendingApplications: number
    pendingStatusChanges: number
}

export interface ConfirmApplicationInput {
    companyId: number
    jobTitle: string
    dateApplied?: string
    status?: string
    jobPostingUrl?: string
    location?: string
    notes?: string
}

export interface ConfirmStatusChangeInput {
    jobApplicationId: number
    status: string
    note?: string
}