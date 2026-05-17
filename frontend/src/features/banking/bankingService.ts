import api from '@/services/api'

export type BankAccountStatus = 'Active' | 'Closed' | 'Suspended'
export type BankTransactionType = 'Debit' | 'Credit'
export type BankTransactionStatus = 'Unreconciled' | 'Reconciled' | 'Voided'

export interface BankAccountDto {
  id: string; accountNumber: string; bankName: string; currencyCode: string
  balance: number; status: BankAccountStatus; iban?: string; swift?: string
  linkedAccountingAccountId?: string; transactionCount: number
  unreconciledCount: number; createdAt: string; updatedAt: string
}

export interface BankTransactionDto {
  id: string; transactionDate: string; description: string; amount: number
  type: BankTransactionType; status: BankTransactionStatus; referenceNumber?: string
  linkedJournalEntryId?: string; createdAt: string
}

export const bankingService = {
  accounts: {
    list: (status?: string) =>
      api.get<BankAccountDto[]>('/bff/banking/accounts', { params: { status } }).then(r => r.data),
    create: (body: {
      accountNumber: string; bankName: string; currencyCode: string
      iban?: string; swift?: string; linkedAccountingAccountId?: string
    }) => api.post<BankAccountDto>('/bff/banking/accounts', body).then(r => r.data),
  },
  transactions: {
    list: (accountId: string, status?: string) =>
      api.get<BankTransactionDto[]>(`/bff/banking/accounts/${accountId}/transactions`, {
        params: { status },
      }).then(r => r.data),
    add: (accountId: string, body: {
      transactionDate: string; description: string; amount: number
      type: string; referenceNumber?: string
    }) => api.post<BankTransactionDto>(`/bff/banking/accounts/${accountId}/transactions`, body).then(r => r.data),
    reconcile: (accountId: string, txId: string, journalEntryId: string) =>
      api.post<BankTransactionDto>(
        `/bff/banking/accounts/${accountId}/transactions/${txId}/reconcile`,
        { journalEntryId },
      ).then(r => r.data),
    unreconcile: (accountId: string, txId: string) =>
      api.post<BankTransactionDto>(`/bff/banking/accounts/${accountId}/transactions/${txId}/unreconcile`).then(r => r.data),
    void: (accountId: string, txId: string) =>
      api.post<BankTransactionDto>(`/bff/banking/accounts/${accountId}/transactions/${txId}/void`).then(r => r.data),
  },
}
