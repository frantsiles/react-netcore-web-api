import api from '@/services/api'

export type AccountType = 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense'
export type EntryStatus = 'Draft' | 'Posted' | 'Reversed'
export type EntrySide = 'Debit' | 'Credit'

export interface AccountDto {
  id: string; accountNumber: string; name: string; type: AccountType
  currencyCode: string; balance: number; isActive: boolean; description?: string
  createdAt: string; updatedAt: string
}

export interface JournalEntryLineDto {
  id: string; accountId: string; accountNumber: string; accountName: string
  side: EntrySide; amount: number; description?: string
}

export interface JournalEntryDto {
  id: string; entryNumber: string; fiscalPeriod: string; entryDate: string
  status: EntryStatus; description: string; referenceType?: string; referenceId?: string
  totalDebits: number; totalCredits: number; isBalanced: boolean
  lines: JournalEntryLineDto[]; createdAt: string; updatedAt: string
}

export const accountingService = {
  accounts: {
    list: (type?: string, isActive?: boolean) =>
      api.get<AccountDto[]>('/bff/accounting/accounts', { params: { type, isActive } }).then(r => r.data),
    create: (body: { accountNumber: string; name: string; type: string; currencyCode: string; description?: string }) =>
      api.post<AccountDto>('/bff/accounting/accounts', body).then(r => r.data),
  },
  entries: {
    search: (fiscalPeriod?: string, status?: string) =>
      api.get<JournalEntryDto[]>('/bff/accounting/journal-entries', { params: { fiscalPeriod, status, take: 100 } }).then(r => r.data),
    create: (body: { fiscalPeriod: string; entryDate: string; description: string; referenceType?: string; referenceId?: string }) =>
      api.post<JournalEntryDto>('/bff/accounting/journal-entries', body).then(r => r.data),
    addLine: (entryId: string, body: { accountId: string; side: string; amount: number; description?: string }) =>
      api.post<JournalEntryDto>(`/bff/accounting/journal-entries/${entryId}/lines`, body).then(r => r.data),
    post: (entryId: string) =>
      api.post<JournalEntryDto>(`/bff/accounting/journal-entries/${entryId}/post`).then(r => r.data),
    reverse: (entryId: string, reversalDate: string) =>
      api.post<JournalEntryDto>(`/bff/accounting/journal-entries/${entryId}/reverse`, { reversalDate }).then(r => r.data),
  },
}
