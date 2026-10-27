import { useEffect, useRef } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { mailboxApi } from '../api/mailboxApi'

export function useMailboxStatus() {
    const queryClient = useQueryClient()

    const query = useQuery({
        queryKey: ['mailbox', 'status'],
        queryFn: mailboxApi.getStatus,
        refetchInterval: (q) => {
            const state = q.state.data?.syncState
            return state && state !== 'Idle' ? 3000 : false
        },
    })

    // When a sync finishes, the pending review items may have changed
    const state = query.data?.syncState
    const previous = useRef(state)
    useEffect(() => {
        if (previous.current && previous.current !== 'Idle' && state === 'Idle') {
            queryClient.invalidateQueries({ queryKey: ['review'] })
        }
        previous.current = state
    }, [state, queryClient])

    return query
}

export function useStartSync() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: mailboxApi.startSync,
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['mailbox', 'status'] }),
    })
}


export function useConnectGmail() {
    return useMutation({
        mutationFn: mailboxApi.getConnectUrl,
        onSuccess: (url) => window.location.assign(url),
    })
}