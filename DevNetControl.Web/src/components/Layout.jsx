import { useState, useEffect } from 'react'
import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import api from '../lib/api'
import {
  LayoutDashboard, Users, Server, Shield, LogOut, Menu, X,
  ChevronDown, ChevronRight, FileText, Settings, Wallet, UserCheck,
  Sun, Moon
} from 'lucide-react'

const adminNavSections = [
  {
    label: 'Dashboard',
    items: [
      { to: '/dashboard', icon: LayoutDashboard, label: 'Inicio' },
    ]
  },
  {
    label: 'Gestion',
    items: [
      { to: '/users', icon: Users, label: 'Usuarios' },
      { to: '/plans', icon: FileText, label: 'Planes' },
      { to: '/nodes', icon: Server, label: 'Nodos' },
    ]
  },
  {
    label: 'Sistema',
    items: [
      { to: '/logs', icon: FileText, label: 'Logs' },
    ]
  },
]

const resellerNavSections = [
  {
    label: 'Dashboard',
    items: [
      { to: '/dashboard', icon: LayoutDashboard, label: 'Inicio' },
    ]
  },
  {
    label: 'Gestion',
    items: [
      { to: '/users', icon: Users, label: 'Usuarios' },
    ]
  },
  {
    label: 'Sistema',
    items: [
      { to: '/logs', icon: FileText, label: 'Logs' },
    ]
  },
]

const adminNavItems = [
  { to: '/resellers', icon: UserCheck, label: 'Resellers' },
]

const adminItems = [
  { to: '/admin', icon: Shield, label: 'Admin Panel' },
  { to: '/superadmin', icon: Settings, label: 'Super Admin' },
]

export default function Layout() {
  const { user, logout, updateCredits } = useAuthStore()
  const navigate = useNavigate()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [collapsedSections, setCollapsedSections] = useState({})
  const [darkMode, setDarkMode] = useState(() => {
    const saved = localStorage.getItem('theme')
    return saved ? saved === 'dark' : true
  })

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', darkMode ? 'dark' : 'light')
    localStorage.setItem('theme', darkMode ? 'dark' : 'light')
  }, [darkMode])

  useEffect(() => {
    async function fetchCredits() {
      try {
        const { data } = await api.get('/credit/balance')
        updateCredits(data.balance)
      } catch (err) {
        console.error('Error fetching credits:', err)
      }
    }
    fetchCredits()
  }, [updateCredits])

  const isSuperAdmin = user?.role === 'SuperAdmin'
  const isAdmin = user?.role === 'Admin' || isSuperAdmin
  const isReseller = user?.role === 'Reseller' || user?.role === 'SubReseller'
  const navSections = isAdmin ? adminNavSections : resellerNavSections

  const toggleSection = (section) => {
    setCollapsedSections(prev => ({ ...prev, [section]: !prev[section] }))
  }

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <div className="min-h-screen flex bg-[var(--bg-primary)]">
      {/* Mobile overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-black/60 z-40 md:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside className={`
        fixed md:sticky top-0 left-0 h-screen w-64 bg-[var(--bg-secondary)] border-r border-[var(--border-color)]
        z-50 flex flex-col transition-transform duration-200
        ${sidebarOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}
      `}>
        {/* Logo */}
        <div className="h-12 px-4 flex items-center justify-between border-b border-[var(--border-color)] shrink-0">
          <div className="flex items-center gap-2">
            <div className="w-6 h-6 bg-blue-500 rounded flex items-center justify-center">
              <Server className="w-3.5 h-3.5 text-white" />
            </div>
            <span className="font-bold text-sm text-[var(--text-primary)]">DevNetControl</span>
          </div>
          <button onClick={() => setSidebarOpen(false)} className="md:hidden text-[var(--text-secondary)]">
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Nav */}
        <nav className="flex-1 overflow-y-auto py-3 px-2">
          {navSections.map((section) => (
            <div key={section.label} className="mb-4">
              <button
                onClick={() => toggleSection(section.label)}
                className="w-full flex items-center justify-between px-2 py-1.5 text-xs font-semibold text-[var(--text-muted)] uppercase tracking-wider hover:text-[var(--text-secondary)]"
              >
                {section.label}
                {collapsedSections[section.label]
                  ? <ChevronRight className="w-3 h-3" />
                  : <ChevronDown className="w-3 h-3" />
                }
              </button>

              {!collapsedSections[section.label] && (
                <div className="mt-1 space-y-0.5">
                  {section.items.map((item) => (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      onClick={() => setSidebarOpen(false)}
                      className={({ isActive }) =>
                        `flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors ${
                          isActive
                            ? 'bg-blue-500/10 text-blue-400 font-medium'
                            : 'text-[var(--text-secondary)] hover:bg-[var(--bg-hover)] hover:text-[var(--text-primary)]'
                        }`
                      }
                    >
                      <item.icon className="w-4 h-4 shrink-0" />
                      {item.label}
                    </NavLink>
                  ))}
                </div>
              )}
            </div>
          ))}

          {(isAdmin || isReseller) && (
            <div className="mt-2 pt-2 border-t border-[var(--border-color)]">
              <div className="px-2 py-1.5 text-xs font-semibold text-[var(--text-muted)] uppercase tracking-wider">
                Resellers
              </div>
              <div className="mt-1 space-y-0.5">
                {adminNavItems.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    onClick={() => setSidebarOpen(false)}
                    className={({ isActive }) =>
                      `flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors ${
                        isActive
                          ? 'bg-blue-500/10 text-blue-400 font-medium'
                          : 'text-[var(--text-secondary)] hover:bg-[var(--bg-hover)] hover:text-[var(--text-primary)]'
                      }`
                    }
                  >
                    <item.icon className="w-4 h-4 shrink-0" />
                    {item.label}
                  </NavLink>
                ))}
              </div>
            </div>
          )}

          {isAdmin && (
            <div className="mt-4 pt-4 border-t border-[var(--border-color)]">
              <div className="px-2 py-1.5 text-xs font-semibold text-[var(--text-muted)] uppercase tracking-wider">
                Administracion
              </div>
              <div className="mt-1 space-y-0.5">
                {adminItems.filter(i => i.to !== '/superadmin' || isSuperAdmin).map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    onClick={() => setSidebarOpen(false)}
                    className={({ isActive }) =>
                      `flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors ${
                        isActive
                          ? 'bg-red-500/10 text-red-400 font-medium'
                          : 'text-[var(--text-secondary)] hover:bg-[var(--bg-hover)] hover:text-[var(--text-primary)]'
                      }`
                    }
                  >
                    <item.icon className="w-4 h-4 shrink-0" />
                    {item.label}
                  </NavLink>
                ))}
              </div>
            </div>
          )}
        </nav>

        {/* User */}
        <div className="p-3 border-t border-[var(--border-color)] shrink-0">
          <div className="flex items-center gap-2 mb-2">
            <div className="w-8 h-8 rounded-full bg-blue-500/20 flex items-center justify-center">
              <span className="text-blue-400 font-semibold text-xs">{user?.userName?.charAt(0).toUpperCase()}</span>
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-[var(--text-primary)] truncate">{user?.userName}</p>
              <p className="text-xs text-[var(--text-muted)]">{user?.role}</p>
            </div>
          </div>
          <button
            onClick={handleLogout}
            className="w-full flex items-center justify-center gap-2 py-2 text-sm text-[var(--text-secondary)] hover:text-[var(--danger)] hover:bg-[var(--bg-hover)] rounded-md transition-colors"
          >
            <LogOut className="w-4 h-4" />
            Cerrar sesion
          </button>
        </div>
      </aside>

      {/* Main */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Top bar */}
        <header className="h-12 px-4 flex items-center justify-between bg-[var(--bg-secondary)] border-b border-[var(--border-color)] shrink-0 sticky top-0 z-30">
          <button onClick={() => setSidebarOpen(true)} className="md:hidden text-[var(--text-secondary)]">
            <Menu className="w-5 h-5" />
          </button>

          <div className="flex-1" />

          <div className="flex items-center gap-4">
            <button
              onClick={() => setDarkMode(!darkMode)}
              className="p-2 rounded-md hover:bg-[var(--bg-hover)] text-[var(--text-secondary)] transition-colors"
              title={darkMode ? 'Modo claro' : 'Modo oscuro'}
            >
              {darkMode ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
            </button>
            {user?.credits !== undefined && (
              <div className="flex items-center gap-2 px-3 py-1.5 bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-md">
                <Wallet className="w-4 h-4 text-yellow-500" />
                <span className="text-sm font-semibold text-[var(--text-primary)]">{user?.credits?.toLocaleString() ?? 0}</span>
              </div>
            )}
            {user?.credits === 0 && (
              <span className="badge badge-danger">Saldo Agotado</span>
            )}
          </div>
        </header>

        {/* Content */}
        <main className="flex-1 p-4 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
