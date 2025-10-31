import React, { createContext, useCallback, useContext, useMemo, useState } from 'react'
import { api, setAccessToken } from '../api'
import type { AuthResponse, User } from '../types'


interface AuthContextValue {
    user?: User
    token?: string
    login: (username: string, password: string) => Promise<void>
    logout: () => Promise<void>
}


const AuthCtx = createContext<AuthContextValue>(null as any)
export const useAuth = () => useContext(AuthCtx)


export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [user, setUser] = useState<User | undefined>()
    const [token, setToken] = useState<string | undefined>()


    const login = useCallback(async (username: string, password: string) => {
        const { data } = await api.post<AuthResponse>('/auth/login', { username, password, device: navigator.userAgent })
        setToken(data.accessToken)
        setAccessToken(data.accessToken)
        setUser(data.user)
    }, [])


    const logout = useCallback(async () => {
        try { await api.post('/auth/logout') } catch { /* ignore */ }
        setToken(undefined)
        setAccessToken(undefined)
        setUser(undefined)
    }, [])


    const value = useMemo(() => ({ user, token, login, logout }), [user, token, login, logout])
    return <AuthCtx.Provider value={value}>{children}</AuthCtx.Provider>
}