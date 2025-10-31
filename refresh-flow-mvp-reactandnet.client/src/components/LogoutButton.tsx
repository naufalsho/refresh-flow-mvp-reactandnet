import { useAuth } from '../auth/AuthContext'


export default function LogoutButton() {
    const { logout } = useAuth()
    return <button onClick={() => logout()}>Logout</button>
}