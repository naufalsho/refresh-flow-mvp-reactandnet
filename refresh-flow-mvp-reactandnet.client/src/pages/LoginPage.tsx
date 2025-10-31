import React, { useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { useNavigate } from 'react-router-dom'


export default function LoginPage() {
    const { login } = useAuth()
    const [u, setU] = useState('')
    const [p, setP] = useState('')
    const [err, setErr] = useState<string>()
    const [busy, setBusy] = useState(false)
    const nav = useNavigate()


    async function onSubmit(e: React.FormEvent) {
        e.preventDefault()
        setErr(undefined)
        setBusy(true)
        try {
            await login(u, p)
            nav('/')
        } catch (e: any) {
            setErr(e?.response?.data?.error ?? 'Login gagal')
        } finally { setBusy(false) }
    }


    return (
        <div style={{ maxWidth: 360, margin: '4rem auto', padding: 16, border: '1px solid #ddd', borderRadius: 8 }}>
            <h2>Login</h2>
            <form onSubmit={onSubmit}>
                <div>
                    <input placeholder="username" value={u} onChange={e => setU(e.target.value)} required />
                </div>
                <div>
                    <input placeholder="password" type="password" value={p} onChange={e => setP(e.target.value)} required />
                </div>
                <button type="submit" disabled={busy}>{busy ? '...' : 'Login'}</button>
            </form>
            {err && <div style={{ color: 'crimson', marginTop: 8 }}>{err}</div>}
        </div>
    )
}