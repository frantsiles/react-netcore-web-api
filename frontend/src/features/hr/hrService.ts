import api from '@/services/api'

export type EmploymentType = 'FullTime' | 'PartTime' | 'Contractor' | 'Temporary'
export type EmployeeStatus = 'Active' | 'OnLeave' | 'Terminated'

export interface DepartmentDto {
  id: string; code: string; name: string; parentDepartmentId?: string
  costCenter?: string; isActive: boolean; createdAt: string; updatedAt: string
}

export interface EmployeeDto {
  id: string; employeeNumber: string; firstName: string; lastName: string; fullName: string
  email: string; phone?: string; departmentId: string; jobTitle: string
  employmentType: EmploymentType; status: EmployeeStatus; hireDate: string
  terminationDate?: string; terminationReason?: string; managerEmployeeId?: string
  createdAt: string; updatedAt: string
}

export const hrService = {
  departments: {
    list: () => api.get<DepartmentDto[]>('/bff/hr/departments').then(r => r.data),
    create: (body: { code: string; name: string; parentDepartmentId?: string; costCenter?: string }) =>
      api.post<DepartmentDto>('/bff/hr/departments', body).then(r => r.data),
  },
  employees: {
    list: (departmentId?: string, status?: string) =>
      api.get<EmployeeDto[]>('/bff/hr/employees', { params: { departmentId, status } }).then(r => r.data),
    hire: (body: {
      firstName: string; lastName: string; email: string; phone?: string
      departmentId: string; jobTitle: string; employmentType: string; hireDate: string
      managerEmployeeId?: string
    }) => api.post<EmployeeDto>('/bff/hr/employees', body).then(r => r.data),
    terminate: (id: string, terminationDate: string, reason: string) =>
      api.post<EmployeeDto>(`/bff/hr/employees/${id}/terminate`, { terminationDate, reason }).then(r => r.data),
  },
}
