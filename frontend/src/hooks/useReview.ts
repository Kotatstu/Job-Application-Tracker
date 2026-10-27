import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { reviewApi } from '../api/reviewApi'
import type { ConfirmApplicationInput, ConfirmStatusChangeInput, ReviewStatus } from '../types/review'

export function useReviewSummary() {
    return useQuery({
        queryKey: ['review', 'summary'],
        queryFn: reviewApi.getSummary,
    })
}

export function usePendingApplications(reviewStatus: ReviewStatus = 'Pending') {
    return useQuery({
        queryKey: ['review', 'applications', reviewStatus],
        queryFn: () => reviewApi.getApplications(reviewStatus),
    })
}

export function usePendingStatusChanges(reviewStatus: ReviewStatus = 'Pending') {
    return useQuery({
        queryKey: ['review', 'statusChanges', reviewStatus],
        queryFn: () => reviewApi.getStatusChanges(reviewStatus),
    })
}

export function useConfirmApplication() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ id, input }: { id: number; input: ConfirmApplicationInput }) =>
            reviewApi.confirmApplication(id, input),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['jobApplications'] }),
        onSettled: () => queryClient.invalidateQueries({ queryKey: ['review'] }),
    })
}

export function useDismissApplication() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (id: number) => reviewApi.dismissApplication(id),
        onSettled: () => queryClient.invalidateQueries({ queryKey: ['review'] }),
    })
}

export function useConfirmStatusChange() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ id, input }: { id: number; input: ConfirmStatusChangeInput }) =>
            reviewApi.confirmStatusChange(id, input),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['jobApplications'] })
            // If your status-history query uses a key that does not start with 'jobApplications',
            // add an invalidation for it here so the timeline refreshes.
        },
        onSettled: () => queryClient.invalidateQueries({ queryKey: ['review'] }),
    })
}

export function useDismissStatusChange() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (id: number) => reviewApi.dismissStatusChange(id),
        onSettled: () => queryClient.invalidateQueries({ queryKey: ['review'] }),
    })
}