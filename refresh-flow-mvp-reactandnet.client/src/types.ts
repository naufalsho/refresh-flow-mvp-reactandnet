export type User = { id: string; username: string; email: string }
export type AuthResponse = { accessToken: string; expiresInSeconds: number; user: User }