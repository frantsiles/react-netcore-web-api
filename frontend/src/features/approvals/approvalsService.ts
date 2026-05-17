import api from '@/services/api'

export type ApprovalRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled'

export interface ApprovalRequestDto {
  id: string; workflowId: string; entityType: string; entityId: string
  entityReference: string; amount?: number; requestedByUserId: string; notes?: string
  status: ApprovalRequestStatus; decidedByUserId?: string; decidedAt?: string
  decisionNotes?: string; createdAt: string; updatedAt: string
}

export const approvalsService = {
  requests: {
    list: (status?: string) =>
      api.get<ApprovalRequestDto[]>('/bff/approvals/requests', { params: { status, take: 100 } }).then(r => r.data),
    approve: (id: string, approverUserId: string, notes?: string) =>
      api.post<ApprovalRequestDto>(`/bff/approvals/requests/${id}/approve`, { approverUserId, notes }).then(r => r.data),
    reject: (id: string, approverUserId: string, notes?: string) =>
      api.post<ApprovalRequestDto>(`/bff/approvals/requests/${id}/reject`, { approverUserId, notes }).then(r => r.data),
  },
}
