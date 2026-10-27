import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { StatusInput } from './StatusInput'
import type { PendingStatusChange } from '@/types/review'
import { useConfirmStatusChange } from '@/hooks/useReview'
import { useJobApplications } from '@/hooks/useJobApplications'
import { EmailPreview } from './EmailPreview'
import { reviewErrorMessage } from '@/types/reviewError'

interface Props {
    item: PendingStatusChange | null
    onClose: () => void
}

export function ConfirmStatusChangeDialog({ item, onClose }: Props) {
    return (
        <Dialog
            open={item !== null}
            onOpenChange={(open) => {
                if (!open) onClose()
            }}
        >
            <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
                {item && <ConfirmStatusChangeForm key={item.id} item={item} onClose={onClose} />}
            </DialogContent>
        </Dialog>
    )
}

function ConfirmStatusChangeForm({ item, onClose }: { item: PendingStatusChange; onClose: () => void }) {
    const confirm = useConfirmStatusChange()
    const { data: applications } = useJobApplications()

    const [applicationId, setApplicationId] = useState(item.suggestedApplication ? String(item.suggestedApplication.id) : '')
    const [status, setStatus] = useState(item.statusGuess)
    const [note, setNote] = useState(item.noteGuess ?? '')

    const canSubmit = applicationId !== '' && status.trim() !== '' && !confirm.isPending

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault()

        confirm.mutate(
            {
                id: item.id,
                input: {
                    jobApplicationId: Number(applicationId),
                    status: status.trim(),
                    note: note.trim() || undefined,
                },
            },
            {
                onSuccess: () => {
                    toast.success('Status updated')
                    onClose()
                },
                onError: (error) => toast.error(reviewErrorMessage(error)),
            },
        )
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <DialogHeader>
                <DialogTitle>Update an application's status</DialogTitle>
                <DialogDescription>Choose the application this email is about, then confirm the new status.</DialogDescription>
            </DialogHeader>

            <EmailPreview email={item.email} />

            <div className="space-y-2">
                <Label>Application</Label>
                <Select value={applicationId} onValueChange={(value) => setApplicationId(value ?? '')}>
                    <SelectTrigger>
                        <SelectValue placeholder="Pick an application" />
                    </SelectTrigger>
                    <SelectContent>
                        {applications?.map((a) => (
                            <SelectItem key={a.id} value={String(a.id)}>
                                {a.companyName} · {a.jobTitle} ({a.currentStatus})
                            </SelectItem>
                        ))}
                    </SelectContent>
                </Select>
            </div>

            <div className="space-y-2">
                <Label htmlFor="newStatus">New status</Label>
                <StatusInput id="newStatus" value={status} onChange={setStatus} />
            </div>

            <div className="space-y-2">
                <Label htmlFor="statusNote">Note (optional)</Label>
                <Textarea id="statusNote" value={note} onChange={(e) => setNote(e.target.value)} />
            </div>

            <DialogFooter>
                <Button type="button" variant="ghost" onClick={onClose}>
                    Cancel
                </Button>
                <Button type="submit" disabled={!canSubmit}>
                    Update status
                </Button>
            </DialogFooter>
        </form>
    )
}