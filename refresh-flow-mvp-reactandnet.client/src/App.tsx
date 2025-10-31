import './App.css'
import { Routes, Route, Link } from 'react-router-dom'
import LoginPage from './pages/LoginPage'
import ProfilePage from './pages/ProfilePage'
import ProtectedRoute from './auth/ProtectedRoute'

export default function App() {
    return (
        <div>
            <nav style={{ padding: 8, borderBottom: '1px solid #eee' }}>
                <Link to="/">Home</Link>{' | '}<Link to="/login">Login</Link>
            </nav>
            <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/" element={
                    <ProtectedRoute>
                        <ProfilePage />
                    </ProtectedRoute>
                } />
            </Routes>
        </div>
    )
}
