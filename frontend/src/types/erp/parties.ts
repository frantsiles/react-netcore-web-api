export type PartyType = 'Individual' | 'Organization'
export type PartyRoleType = 'Customer' | 'Supplier' | 'Employee' | 'Contact'
export type PartyRoleStatus = 'Active' | 'Inactive'

export interface PartyRoleDto {
  roleType: PartyRoleType
  status: PartyRoleStatus
  creditLimit?: number
  creditLimitCurrency?: string
  paymentTermsDays?: number
  employeeNumber?: string
}

export interface AddressDto {
  line1: string
  line2?: string
  city: string
  stateOrRegion: string
  postalCode: string
  countryCode: string
  isPrimary: boolean
}

export interface ContactPointDto {
  type: string
  value: string
  isPrimary: boolean
}

export interface PartyDto {
  partyId: string
  legalName: string
  tradeName?: string
  partyType: PartyType
  countryCode: string
  taxId?: string
  isActive: boolean
  roles: PartyRoleDto[]
  addresses: AddressDto[]
  contactPoints: ContactPointDto[]
}

export interface SearchPartiesParams {
  legalName?: string
  roleType?: PartyRoleType
  isActive?: boolean
  skip?: number
  take?: number
}

export interface RegisterPartyBody {
  legalName: string
  tradeName?: string
  partyType: PartyType
  countryCode: string
  firstRoleType: PartyRoleType
  taxId?: string
  creditLimit?: number
  creditLimitCurrency?: string
  paymentTermsDays?: number
  employeeNumber?: string
}

export interface UpdatePartyProfileBody {
  legalName: string
  tradeName?: string
  taxId?: string
}
