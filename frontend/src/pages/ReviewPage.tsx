import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { buttonVariants } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { usePendingApplications, usePendingStatusChanges } from '../hooks/useReview'
import type { PendingApplication, PendingStatusChange } from '../types/review'
import { PendingApplicationCard } from './PendingApplicationCard'
import { PendingStatusChangeCard } from './PendingStatusChangeCard'
import { ConfirmApplicationDialog } from './ConfirmApplicationDialog'
import { ConfirmStatusChangeDialog } from './ConfirmStatusChangeDialog'

function EmptyState({ text }: { text: string }) {
    return (
        <div className="rounded-md border border-dashed bg-background p-8 text-center">
            <p className="text-sm text-muted-foreground">{text}</p>
            <Link to="/mailbox" className={buttonVariants({ variant: 'outline', size: 'sm', className: 'mt-4' })}>
                Go to Mailbox
            </Link>
        </div>
    )
}

function ListSkeleton() {
    return (
        <div className="space-y-4">
            <Skeleton className="h-40 w-full" />
            <Skeleton className="h-40 w-full" />
        </div>
    )
}

export function ReviewPage() {
    const applications = usePendingApplications()
    const statusChanges = usePendingStatusChanges()

    const [applicationItem, setApplicationItem] = useState<PendingApplication | null>(null)
    const [statusChangeItem, setStatusChangeItem] = useState<PendingStatusChange | null>(null)

    const applicationCount = applications.data?.length ?? 0
    const statusChangeCount = statusChanges.data?.length ?? 0

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-semibold">Review</h1>
                <p className="text-sm text-muted-foreground">
                    Suggestions found in your email. Nothing is added to your applications until you confirm it.
                </p>
            </div>

            <Tabs defaultValue="applications">
                <TabsList>
                    <TabsTrigger value="applications" className="gap-2">
                        New applications
                        {applicationCount > 0 && <Badge variant="secondary">{applicationCount}</Badge>}
                    </TabsTrigger>
                    <TabsTrigger value="statusChanges" className="gap-2">
                        Status changes
                        {statusChangeCount > 0 && <Badge variant="secondary">{statusChangeCount}</Badge>}
                    </TabsTrigger>
                </TabsList>

                <TabsContent value="applications" className="space-y-4">
                    {applications.isPending ? (
                        <ListSkeleton />
                    ) : applications.isError ? (
                        <Alert variant="destructive">
                            <AlertTitle>Could not load suggestions</AlertTitle>
                            <AlertDescription>Check that the backend is running, then refresh.</AlertDescription>
                        </Alert>
                    ) : applicationCount === 0 ? (
                        <EmptyState text="No new applications waiting. They appear after a mailbox check." />
                    ) : (
                        applications.data.map((item) => (
                            <PendingApplicationCard key={item.id} item={item} onReview={setApplicationItem} />
                        ))
                    )}
                </TabsContent>

                <TabsContent value="statusChanges" className="space-y-4">
                    {statusChanges.isPending ? (
                        <ListSkeleton />
                    ) : statusChanges.isError ? (
                        <Alert variant="destructive">
                            <AlertTitle>Could not load suggestions</AlertTitle>
                            <AlertDescription>Check that the backend is running, then refresh.</AlertDescription>
                        </Alert>
                    ) : statusChangeCount === 0 ? (
                        <EmptyState text="No status changes waiting. They appear when an email is about an application you already track." />
                    ) : (
                        statusChanges.data.map((item) => (
                            <PendingStatusChangeCard key={item.id} item={item} onReview={setStatusChangeItem} />
                        ))
                    )}
                </TabsContent>
            </Tabs>

            <ConfirmApplicationDialog item={applicationItem} onClose={() => setApplicationItem(null)} />
            <ConfirmStatusChangeDialog item={statusChangeItem} onClose={() => setStatusChangeItem(null)} />
        </div>
    )
}