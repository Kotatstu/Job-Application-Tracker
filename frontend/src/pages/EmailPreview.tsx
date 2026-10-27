import type { EmailContext } from '../types/review'

export function EmailPreview({ email }: { email: EmailContext }) {
    return (
        <div className="rounded-md border bg-muted/40 p-3 text-sm">
            <div className="font-medium">{email.subject || '(no subject)'}</div>
            <div className="text-xs text-muted-foreground">
                {email.senderEmail} · {new Date(email.receivedAt).toLocaleString()}
            </div>
            {email.snippet && (
                <p className="mt-2 line-clamp-4 whitespace-pre-line text-muted-foreground">{email.snippet}</p>
            )}
        </div>
    )
}