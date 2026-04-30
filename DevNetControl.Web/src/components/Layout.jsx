import { Outlet, NavLink, useLocation } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { Home, Users, CreditCard, Server, Shield, LogOut } from 'lucide-react'

const navItems = [
  { to: '/dashboard', icon: Home, label: 'Inicio' },
  { to: '/users', icon: Users, label: 'Usuarios' },
  { to: '/credits', icon: CreditCard, label: 'Créditos' },
  { to: '/nodes', icon: Server, label: 'Nodos' },
]

export default function Layout() {
  const { user, logout } = useAuthStore()
  const location = useLocation()
  const isAdmin = user?.role === 'Admin'

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      {/* Header */}
      <header className="bg-primary-800 text-white px-4 py-3 flex items-center justify-between sticky top-0 z-50 shadow-md">
        <div className="flex items-center gap-2">
          <Server className="w-5 h-5" />
          <h1 className="font-bold text-lg">DevNetControl</h1>
        </div>
        <div className="flex items-center gap-3">
          <span className="text-xs text-primary-200 hidden sm:inline">{user?.role}</span>
          <button
            onClick={logout}
            className="p-2 rounded-lg hover:bg-primary-700 transition-colors"
            aria-label="Cerrar sesión"
          >
            <LogOut className="w-5 h-5" />
          </button>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1 pb-20 md:pb-4">
        <div className="max-w-4xl mx-auto p-4">
          <Outlet />
        </div>
      </main>

      {/* Desktop Sidebar */}
      <aside className="hidden md:flex fixed left-0 top-14 bottom-0 w-52 bg-white border-r border-gray-200 flex-col py-4">
        <nav className="flex-1 px-2 space-y-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-gray-600 hover:bg-gray-100'
                }`
              }
            >
              <item.icon className="w-5 h-5" />
              {item.label}
            </NavLink>
          ))}
          {isAdmin && (
            <NavLink
              to="/admin"
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-red-50 text-red-700'
                    : 'text-gray-600 hover:bg-gray-100'
                }`
              }
            >
              <Shield className="w-5 h-5" />
              Admin
            </NavLink>
          )}
        </nav>
      </aside>

      {/* Mobile Bottom Navigation */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 bg-white border-t border-gray-200 flex justify-around py-2 z-50 safe-area-bottom">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              `flex flex-col items-center gap-0.5 px-3 py-1.5 rounded-lg transition-colors min-w-0 ${
                isActive
                  ? 'text-primary-600'
                  : 'text-gray-400'
              }`
            }
          >
            <item.icon className="w-5 h-5" />
            <span className="text-[10px] font-medium truncate">{item.label}</span>
          </NavLink>
        ))}
        {isAdmin && (
          <NavLink
            to="/admin"
            className={({ isActive }) =>
              `flex flex-col items-center gap-0.5 px-3 py-1.5 rounded-lg transition-colors min-w-0 ${
                isActive
                  ? 'text-red-600'
                  : 'text-gray-400'
              }`
            }
          >
            <Shield className="w-5 h-5" />
            <span className="text-[10px] font-medium truncate">Admin</span>
          </NavLink>
        )}
      </nav>
    </div>
  )
}
