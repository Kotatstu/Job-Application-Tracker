import { Input } from '@/components/ui/input'

const SUGGESTIONS = ['Applied', 'Interview', 'Offer', 'Rejected']

export function StatusInput({ id, value, onChange }: { id: string; value: string; onChange: (value: string) => void }) {
    return (
        <>
            <Input id={id} list={`${id}-options`} value={value} onChange={(e) => onChange(e.target.value)} />
            <datalist id={`${id}-options`}>
                {SUGGESTIONS.map((s) => (
                    <option key={s} value={s} />
                ))}
            </datalist>
        </>
    )
}