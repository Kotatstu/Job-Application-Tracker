import { api } from './axiosInstance'
import type { JobApplication } from '../types/jobApplication'
import type {
    ConfirmApplicationInput,
    ConfirmStatusChangeInput,
    PendingApplication,
    PendingStatusChange,
    ReviewStatus,
    ReviewSummary,
} from '../types/review'

export const reviewApi = {
    getSummary: async (): Promise<ReviewSummary> => {
        const response = await api.get<ReviewSummary>('/review/summary')
        return response.data
    },

    getApplications: async (reviewStatus: ReviewStatus = 'Pending'): Promise<PendingApplication[]> => {
        const response = await api.get<PendingApplication[]>('/review/applications', { params: { reviewStatus } })
        return response.data
    },

    confirmApplication: async (id: number, input: ConfirmApplicationInput): Promise<JobApplication> => {
        const response = await api.post<JobApplication>(`/review/applications/${id}/confirm`, input)
        return response.data
    },

    dismissApplication: async (id: number): Promise<void> => {
        await api.post(`/review/applications/${id}/dismiss`)
    },

    getStatusChanges: async (reviewStatus: ReviewStatus = 'Pending'): Promise<PendingStatusChange[]> => {
        const response = await api.get<PendingStatusChange[]>('/review/status-changes', { params: { reviewStatus } })
        return response.data
    },

    confirmStatusChange: async (id: number, input: ConfirmStatusChangeInput): Promise<JobApplication> => {
        const response = await api.post<JobApplication>(`/review/status-changes/${id}/confirm`, input)
        return response.data
    },

    dismissStatusChange: async (id: number): Promise<void> => {
        await api.post(`/review/status-changes/${id}/dismiss`)
    },
}