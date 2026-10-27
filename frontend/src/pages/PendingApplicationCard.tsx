import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import type { PendingApplication } from '@/types/review'
import { useDismissApplication } from '@/hooks/useReview'
import { reviewErrorMessage } from '@/types/reviewError'
import { EmailPreview } from './EmailPreview'

interface Props {
    item: PendingApplication
    onReview: (item: PendingApplication) => void
}

export function PendingApplicationCard({ item, onReview }: Props) {
    const dismiss = useDismissApplication()

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
                        <CardTitle className="text-base">{item.companyNameGuess}</CardTitle>
                        <p className="text-sm text-muted-foreground">{item.jobTitleGuess ?? 'Job title not found'}</p>
                    </div>
                    {item.statusGuess && <Badge variant="secondary">{item.statusGuess}</Badge>}
                </div>
            </CardHeader>
            <CardContent>
                <EmailPreview email={item.email} />
            </CardContent>
            <CardFooter className="gap-2">
                <Button size="sm" onClick={() => onReview(item)}>
                    Review and add
                </Button>
                <Button size="sm" variant="ghost" onClick={handleDismiss} disabled={dismiss.isPending}>
                    Dismiss
                </Button>
            </CardFooter>
        </Card>
    )
}