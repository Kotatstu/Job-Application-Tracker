import { Outlet, Link, useNavigate } from 'react-router-dom'
import { Badge, Briefcase, Inbox, LogOut, Plus } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { useAuth } from '../hooks/useAuth'
import { buttonVariants } from '@/components/ui/button'
import { useReviewSummary } from '@/hooks/useReview'

export function Layout() {
    const { user, logout } = useAuth()
    const navigate = useNavigate()
    const { data: summary } = useReviewSummary()
    const pending = (summary?.pendingApplications ?? 0) + (summary?.pendingStatusChanges ?? 0)


    const handleLogout = async () => {
        await logout()
        toast.success('Logged out')
        navigate('/login')
    }

    return (
        <div className="min-h-screen bg-muted/30">
            <header className="border-b bg-background sticky top-0 z-10">
                <div className="max-w-5xl mx-auto px-6 py-3 flex items-center justify-between">
                    <Link to="/" className="flex items-center gap-2 font-semibold text-lg">
                        <Briefcase className="h-5 w-5 text-primary" />
                        Job Tracker
                    </Link>

                    <div className="flex items-center gap-3">
                        <Link to="/applications/new" className={buttonVariants({ size: 'sm' })}>
                            <Plus className="h-4 w-4" />
                            New Application
                        </Link>

                        <Link to="/review" className={buttonVariants({ variant: 'ghost', size: 'sm' })}>
                            <Inbox className="h-4 w-4" />
                            Review
                            {pending > 0 && <Badge>{pending}</Badge>}
                        </Link>

                        <span className="text-sm text-muted-foreground hidden sm:inline">
                            {user?.email}
                        </span>

                        <Button variant="ghost" size="sm" onClick={handleLogout}>
                            <LogOut className="h-4 w-4" />
                            Log out
                        </Button>
                    </div>
                </div>
            </header>

            <main className="max-w-5xl mx-auto px-6 py-8">
                <Outlet />
            </main>
        </div>
    )
}