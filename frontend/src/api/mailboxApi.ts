import { api } from './axiosInstance'
import type { MailboxStatus } from '../types/mailbox'

export const mailboxApi = {
    getStatus: async (): Promise<MailboxStatus> => {
        const response = await api.get<MailboxStatus>('/mailbox/status')
        return response.data
    },

    // Returns 202 straight away; the sync runs on the server
    startSync: async (): Promise<void> => {
        await api.post('/mailbox/sync')
    },

    getConnectUrl: async (): Promise<string> => {
        const response = await api.get<{ url: string }>('/mailbox/connectUrl')
        return response.data.url
    },
}