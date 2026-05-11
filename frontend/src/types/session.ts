export interface Session {
  sessionId: string;
  userAgent: string;
  ipAddress: string;
  createdAt: string;
  lastUsedAt: string;
  expiresAt: string;
  isActive: boolean;
  isCurrent: boolean;
}

export interface AdminSession {
  sessionId: string;
  userId: string;
  userEmail: string;
  userFullName: string;
  userAgent: string;
  ipAddress: string;
  createdAt: string;
  lastUsedAt: string;
}
