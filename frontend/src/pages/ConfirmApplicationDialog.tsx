import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
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
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { companiesApi } from '../api/companiesApi'
import { useConfirmApplication } from '../hooks/useReview'
import type { PendingApplication } from '../types/review'
import { CompanyPicker } from '../components/CompanyPicker'
import { StatusInput } from './StatusInput'
import { EmailPreview } from './EmailPreview'
import { reviewErrorMessage } from '@/types/reviewError'

interface Props {
    item: PendingApplication | null
    onClose: () => void
}

export function ConfirmApplicationDialog({ item, onClose }: Props) {
    return (
        <Dialog
            open={item !== null}
            onOpenChange={(open) => {
                if (!open) onClose()
            }}
        >
            <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
                {/* key remounts the form for each item, so its fields start from that item's guesses */}
                {item && <ConfirmApplicationForm key={item.id} item={item} onClose={onClose} />}
            </DialogContent>
        </Dialog>
    )
}

function toDateInput(iso: string): string {
    const d = new Date(iso)
    const pad = (n: number) => String(n).padStart(2, '0')
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

function ConfirmApplicationForm({ item, onClose }: { item: PendingApplication; onClose: () => void }) {
    const queryClient = useQueryClient()
    const confirm = useConfirmApplication()
    const { data: companies } = useQuery({ queryKey: ['companies'], queryFn: companiesApi.getAll })

    const [companyId, setCompanyId] = useState<number | null>(null)
    const [jobTitle, setJobTitle] = useState(item.jobTitleGuess ?? '')
    const [dateApplied, setDateApplied] = useState(toDateInput(item.email.receivedAt))
    const [status, setStatus] = useState(item.statusGuess ?? 'Applied')
    const [jobPostingUrl, setJobPostingUrl] = useState('')
    const [location, setLocation] = useState('')
    const [notes, setNotes] = useState('')

    const suggestion = item.companyNameGuess.trim()
    const hasSuggestion = suggestion !== '' && suggestion.toLowerCase() !== 'unknown'
    const existing = companies?.find((c) => c.companyName.toLowerCase() === suggestion.toLowerCase())

    const createCompany = useMutation({
        mutationFn: companiesApi.create,
        onSuccess: (result) => {
            queryClient.invalidateQueries({ queryKey: ['companies'] })
            setCompanyId(result.company.id)
        },
    })

    const applySuggestion = () => {
        if (existing) setCompanyId(existing.id)
        else createCompany.mutate(suggestion)
    }

    const canSubmit =
        companyId !== null && jobTitle.trim() !== '' && dateApplied !== '' && status.trim() !== '' && !confirm.isPending

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault()
        if (companyId === null) return

        confirm.mutate(
            {
                id: item.id,
                input: {
                    companyId,
                    jobTitle: jobTitle.trim(),
                    dateApplied,
                    status: status.trim(),
                    jobPostingUrl: jobPostingUrl.trim() || undefined,
                    location: location.trim() || undefined,
                    notes: notes.trim() || undefined,
                },
            },
            {
                onSuccess: () => {
                    toast.success('Application added')
                    onClose()
                },
                onError: (error) => toast.error(reviewErrorMessage(error)),
            },
        )
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <DialogHeader>
                <DialogTitle>Add this application</DialogTitle>
                <DialogDescription>Check the guesses, fix anything wrong, then confirm.</DialogDescription>
            </DialogHeader>

            <EmailPreview email={item.email} />

            <div className="space-y-2">
                <Label>Company</Label>
                <CompanyPicker value={companyId} onChange={(id) => setCompanyId(id)} />
                {hasSuggestion && companyId === null && (
                    <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={applySuggestion}
                        disabled={createCompany.isPending}
                    >
                        {existing ? `Use "${existing.companyName}"` : `Add "${suggestion}" as a new company`}
                    </Button>
                )}
            </div>

            <div className="space-y-2">
                <Label htmlFor="jobTitle">Job title</Label>
                <Input id="jobTitle" value={jobTitle} onChange={(e) => setJobTitle(e.target.value)} />
            </div>

            <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                    <Label htmlFor="dateApplied">Date applied</Label>
                    <Input
                        id="dateApplied"
                        type="date"
                        value={dateApplied}
                        onChange={(e) => setDateApplied(e.target.value)}
                    />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="status">Status</Label>
                    <StatusInput id="status" value={status} onChange={setStatus} />
                </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                    <Label htmlFor="jobPostingUrl">Job posting link (optional)</Label>
                    <Input id="jobPostingUrl" value={jobPostingUrl} onChange={(e) => setJobPostingUrl(e.target.value)} />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="location">Location (optional)</Label>
                    <Input id="location" value={location} onChange={(e) => setLocation(e.target.value)} />
                </div>
            </div>

            <div className="space-y-2">
                <Label htmlFor="notes">Notes (optional)</Label>
                <Textarea id="notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
            </div>

            <DialogFooter>
                <Button type="button" variant="ghost" onClick={onClose}>
                    Cancel
                </Button>
                <Button type="submit" disabled={!canSubmit}>
                    Add application
                </Button>
            </DialogFooter>
        </form>
    )
}