import { useEffect, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { isAxiosError } from 'axios'
import { toast } from 'sonner'
import { Loader2, Mail, RefreshCw } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { useConnectGmail, useMailboxStatus, useStartSync } from '../hooks/useMailbox'

const callbackMessages: Record<string, { ok: boolean; text: string }> = {
    connected: { ok: true, text: 'Gmail connected' },
    denied: { ok: false, text: 'Access was denied on the Google screen' },
    norefresh: {
        ok: false,
        text: 'Google did not return a refresh token. Remove this app from your Google account permissions, then connect again',
    },
    invalid: { ok: false, text: 'The connection link expired or was already used. Check the status below or try again' },
    failed: { ok: false, text: 'Connecting failed. Please try again' },
}

function formatDate(iso: string | null): string {
    return iso ? new Date(iso).toLocaleString() : 'Never'
}

export function MailboxPage() {
    const [searchParams, setSearchParams] = useSearchParams()
    const { data: status, isPending, isError, dataUpdatedAt } = useMailboxStatus()
    const startSync = useStartSync()
    const connect = useConnectGmail()

    // Show the callback toast once, then clear the query string (ref guards React StrictMode's double effect)
    const handled = useRef(false)
    useEffect(() => {
        const result = searchParams.get('status')
        if (!result || handled.current) return
        handled.current = true

        const message = callbackMessages[result]
        if (message) {
            if (message.ok) toast.success(message.text)
            else toast.error(message.text)
        }
        setSearchParams({}, { replace: true })
    }, [searchParams, setSearchParams])

    // Live countdown for the cooldown the server reported when the status was last fetched
    const cooldownEndsAt =
        status && status.cooldownRemainingSeconds > 0 ? dataUpdatedAt + status.cooldownRemainingSeconds * 1000 : 0
    const [now, setNow] = useState(() => Date.now())
    const cooling = cooldownEndsAt > now
    useEffect(() => {
        if (!cooling) return
        const id = setInterval(() => setNow(Date.now()), 1000)
        return () => clearInterval(id)
    }, [cooling])
    const secondsLeft = Math.max(0, Math.ceil((cooldownEndsAt - now) / 1000))

    const handleSync = () => {
        startSync.mutate(undefined, {
            onSuccess: () => toast.success('Sync started'),
            onError: (error) => {
                const code = isAxiosError(error) ? error.response?.status : undefined
                if (code === 409) toast.info('A sync is already running')
                else if (code === 429) toast.info('Please wait a moment before checking again')
                else if (code === 400) toast.error('No Gmail account is connected')
                else toast.error('Could not start the sync')
            },
        })
    }

    if (isPending) {
        return (
            <div className="space-y-4">
                <Skeleton className="h-8 w-48" />
                <Skeleton className="h-48 w-full" />
            </div>
        )
    }

    if (isError || !status) {
        return (
            <Alert variant="destructive">
                <AlertTitle>Could not load the mailbox status</AlertTitle>
                <AlertDescription>Check that the backend is running, then refresh the page.</AlertDescription>
            </Alert>
        )
    }

    const busy = status.syncState !== 'Idle'
    const summary = status.lastSummary

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-semibold">Mailbox</h1>
                <p className="text-sm text-muted-foreground">
                    Connect Gmail so new application emails can be found and suggested for review. Nothing is added to your
                    applications until you confirm it.
                </p>
            </div>

            {!status.connected ? (
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Mail className="h-5 w-5" />
                            Connect Gmail
                        </CardTitle>
                        <CardDescription>
                            Read-only access to your inbox. The refresh token is stored encrypted on your own machine.
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <Button onClick={() => connect.mutate()} disabled={connect.isPending}>
                            {connect.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
                            Connect Gmail
                        </Button>
                    </CardContent>
                </Card>
            ) : (
                <>
                    {status.needsReconnect && (
                        <Alert variant="destructive">
                            <AlertTitle>Reconnect needed</AlertTitle>
                            <AlertDescription>
                                <p>
                                    Google no longer accepts the saved connection (it expires after 7 days while the app is in
                                    testing mode). Reconnecting starts fresh from today, so mail from the gap is skipped.
                                </p>
                                <Button
                                    size="sm"
                                    className="mt-3"
                                    onClick={() => connect.mutate()}
                                    disabled={connect.isPending}
                                >
                                    Reconnect Gmail
                                </Button>
                            </AlertDescription>
                        </Alert>
                    )}

                    <Card>
                        <CardHeader>
                            <div className="flex items-start justify-between gap-4">
                                <div>
                                    <CardTitle className="flex items-center gap-2">
                                        <Mail className="h-5 w-5" />
                                        {status.googleEmail}
                                    </CardTitle>
                                    <CardDescription>Connected {formatDate(status.connectedAt)}</CardDescription>
                                </div>
                                <Badge variant={busy ? 'default' : 'secondary'}>
                                    {status.syncState === 'Idle' ? 'Idle' : status.syncState === 'Queued' ? 'Queued' : 'Syncing'}
                                </Badge>
                            </div>
                        </CardHeader>

                        <CardContent className="space-y-4">
                            <div className="text-sm">
                                <span className="text-muted-foreground">Last synced: </span>
                                {formatDate(status.lastSyncedAt)}
                            </div>

                            <div className="flex flex-wrap items-center gap-3">
                                <Button
                                    onClick={handleSync}
                                    disabled={busy || cooling || status.needsReconnect || startSync.isPending}
                                >
                                    {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}
                                    {busy ? 'Syncing...' : cooling ? `Check again in ${secondsLeft}s` : 'Check now'}
                                </Button>
                                {busy && (
                                    <span className="text-sm text-muted-foreground">
                                        This can take several minutes. The local model reads every email.
                                        {status.runStartedAt && ` Started ${formatDate(status.runStartedAt)}.`}
                                    </span>
                                )}
                            </div>
                        </CardContent>
                    </Card>

                    {status.lastError && (
                        <Alert variant="destructive">
                            <AlertTitle>The last sync failed</AlertTitle>
                            <AlertDescription>{status.lastError}</AlertDescription>
                        </Alert>
                    )}

                    {summary && summary.readStatus === 0 && (
                        <Card>
                            <CardHeader>
                                <CardTitle className="text-base">Last run</CardTitle>
                                <CardDescription>Finished {formatDate(status.lastRunFinishedAt)}</CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
                                    {[
                                        ['Emails read', summary.read],
                                        ['Not job related', summary.notJobRelated],
                                        ['New applications', summary.newApplications],
                                        ['Status changes', summary.statusChanges],
                                        ['No action', summary.noAction],
                                        ['Failed', summary.failed],
                                    ].map(([label, value]) => (
                                        <div key={label}>
                                            <div className="text-2xl font-semibold">{value}</div>
                                            <div className="text-xs text-muted-foreground">{label}</div>
                                        </div>
                                    ))}
                                </div>

                                {!summary.checkpointAdvanced && (
                                    <>
                                        <Separator />
                                        <p className="text-sm text-muted-foreground">
                                            {summary.failed > 0
                                                ? 'Some emails failed, so they will be retried on the next check.'
                                                : 'More mail is waiting. Run another check to continue.'}
                                        </p>
                                    </>
                                )}
                            </CardContent>
                        </Card>
                    )}
                </>
            )}
        </div>
    )
}