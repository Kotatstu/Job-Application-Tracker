import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import type { PendingStatusChange } from '@/types/review'
import { useDismissStatusChange } from '@/hooks/useReview'
import { reviewErrorMessage } from '@/types/reviewError'
import { EmailPreview } from './EmailPreview'


interface Props {
    item: PendingStatusChange
    onReview: (item: PendingStatusChange) => void
}

export function PendingStatusChangeCard({ item, onReview }: Props) {
    const dismiss = useDismissStatusChange()
    const app = item.suggestedApplication

    const handleDismiss = () => {
        dismiss.mutate(item.id, {
            onSuccess: () => toast.success('Dismissed'),
            onError: (error) => toast.error(reviewErrorMessage(error)),
        })
    }

    return (
        <Card>
            <CardHeader>
                <div className="flex items-start justify-between gap-3">
                    <div>
                        <CardTitle className="text-base">
                            {app ? `${app.companyName} · ${app.jobTitle}` : 'No matching application'}
                        </CardTitle>
                        <p className="text-sm text-muted-foreground">
                            {app ? `Currently ${app.currentStatus}` : 'You will pick the application when you review'}
                        </p>
                    </div>
                    <Badge variant="secondary">→ {item.statusGuess}</Badge>
                </div>
            </CardHeader>
            <CardContent>
                <EmailPreview email={item.email} />
            </CardContent>
            <CardFooter className="gap-2">
                <Button size="sm" onClick={() => onReview(item)}>
                    Review and update
                </Button>
                <Button size="sm" variant="ghost" onClick={handleDismiss} disabled={dismiss.isPending}>
                    Dismiss
                </Button>
            </CardFooter>
        </Card>
    )
}