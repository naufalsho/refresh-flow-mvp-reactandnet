import { useEffect, useState } from 'react'
import { api } from '../api'
import { useAuth } from '../auth/AuthContext'
import LogoutButton from '../components/LogoutButton'


export default function ProfilePage() {
    const { user, token } = useAuth()
    const [me, setMe] = useState(user)
    const [err, setErr] = useState<string>()


    useEffect(() => {
        let mounted = true
        async function load() {
            try {
                const { data } = await api.get('/me', { headers: token ? { Authorization: `Bearer ${token}` } : undefined })
                if (mounted) setMe(data)
            } catch (e: any) {
                setErr(e?.response?.data?.error ?? 'Gagal memuat profil')
            }
        }
        load()
        return () => { mounted = false }
    }, [token])


    return (
        <div style={{ maxWidth: 640, margin: '2rem auto', padding: 16 }}>
            <h2>Profile</h2>
            {me ? (
                <pre>{JSON.stringify(me, null, 2)}</pre>
            ) : err ? (
                <div style={{ color: 'crimson' }}>{err}</div>
            ) : (
                <div>Loading...</div>
            )}
            <div style={{ marginTop: 16 }}>
                <LogoutButton />
            </div>
        </div>
    )
}