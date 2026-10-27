import { isAxiosError } from 'axios'

export function reviewErrorMessage(error: unknown): string {
    if (isAxiosError(error)) {
        const message = error.response?.data?.message
        if (typeof message === 'string') return message
    }
    return 'Something went wrong. Please try again.'
}