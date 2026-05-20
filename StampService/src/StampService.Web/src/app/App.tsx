import { LogOut, Settings, WalletCards, Workflow } from 'lucide-react';
import { useState, type ReactNode } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import { PhoneLoginPage } from '../auth/PhoneLoginPage';
import { ProfilePage } from '../profile/ProfilePage';
import { WalletPage } from '../wallet/WalletPage';
import { navigationLabels } from './navigationLabels';

export function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginRoute />} />
          <Route
            path="/*"
            element={
              <RequireAuth>
                <AppShell />
              </RequireAuth>
            }
          />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

function LoginRoute() {
  const auth = useAuth();

  if (auth.isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <PhoneLoginPage />;
}

function RequireAuth({ children }: { children: ReactNode }) {
  const auth = useAuth();

  if (!auth.isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return children;
}

function AppShell() {
  const auth = useAuth();
  const [activeSection, setActiveSection] = useState<'profile' | 'wallet'>('wallet');
  const pageTitle = activeSection === 'profile'
    ? navigationLabels.accountSettings
    : navigationLabels.myWallet;
  const pageDescription = activeSection === 'profile'
    ? 'Профиль, способы входа и привязка контактов.'
    : 'Балансы, доступные награды и код для списания.';

  return (
    <div className="app-shell">
      <aside className="sidebar" aria-label="Основное меню">
        <div className="sidebar__brand">StampService</div>
        <nav className="sidebar__nav">
          <button
            className={`sidebar__item ${activeSection === 'wallet' ? 'sidebar__item--active' : ''}`}
            type="button"
            onClick={() => setActiveSection('wallet')}
          >
            <WalletCards size={18} />
            {navigationLabels.myWallet}
          </button>
          <button className="sidebar__item" type="button" disabled>
            <Workflow size={18} />
            Рабочие бренды
          </button>
          <button
            className={`sidebar__item ${activeSection === 'profile' ? 'sidebar__item--active' : ''}`}
            type="button"
            onClick={() => setActiveSection('profile')}
          >
            <Settings size={18} />
            {navigationLabels.accountSettings}
          </button>
        </nav>
      </aside>

      <main className="workspace">
        <header className="workspace__header">
          <div>
            <h1>{pageTitle}</h1>
            <p>{pageDescription}</p>
          </div>
          <button className="button-secondary" type="button" onClick={auth.signOut}>
            <LogOut size={18} />
            Выйти
          </button>
        </header>

        {activeSection === 'profile' ? <ProfilePage /> : <WalletPage />}
      </main>
    </div>
  );
}
