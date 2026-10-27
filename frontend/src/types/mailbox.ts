export type SyncState = 'Idle' | 'Queued' | 'Running'

export interface SyncSummary {
    readStatus: number
    read: number
    alreadyDone: number
    notJobRelated: number
    jobRelated: number
    newApplications: number
    statusChanges: number
    noAction: number
    failed: number
    checkpointAdvanced: boolean
}

export interface MailboxStatus {
    connected: boolean
    googleEmail: string | null
    connectedAt: string | null
    lastSyncedAt: string | null
    syncState: SyncState
    runStartedAt: string | null
    lastRunFinishedAt: string | null
    lastReadStatus: string | null
    lastSummary: SyncSummary | null
    lastError: string | null
    needsReconnect: boolean
    cooldownRemainingSeconds: number
}