import api from '@/services/api'
import type {
  PartyDto,
  SearchPartiesParams,
  RegisterPartyBody,
  UpdatePartyProfileBody,
} from '@/types/erp/parties'

export const partiesService = {
  search: (params: SearchPartiesParams) =>
    api.get<PartyDto[]>('/bff/parties', { params }).then(r => r.data),

  getById: (id: string) =>
    api.get<PartyDto>(`/bff/parties/${id}`).then(r => r.data),

  register: (body: RegisterPartyBody) =>
    api.post<PartyDto>('/bff/parties', body).then(r => r.data),

  updateProfile: (id: string, body: UpdatePartyProfileBody) =>
    api.patch<PartyDto>(`/bff/parties/${id}/profile`, body).then(r => r.data),

  reactivate: (id: string) =>
    api.post(`/bff/parties/${id}/reactivate`),

  deactivate: (id: string) =>
    api.delete(`/bff/parties/${id}`),
}
