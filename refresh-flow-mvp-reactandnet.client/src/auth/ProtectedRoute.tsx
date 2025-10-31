import React from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from './AuthContext'


export default function ProtectedRoute({ children }: { children: React.ReactElement }) {
    const { token } = useAuth()
    if (!token) return <Navigate to="/login" replace />
    return children
}