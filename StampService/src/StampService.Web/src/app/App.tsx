import { LogOut, UserRound, WalletCards, Workflow } from 'lucide-react';
import type { ReactNode } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import { PhoneLoginPage } from '../auth/PhoneLoginPage';
import { WalletPage } from '../wallet/WalletPage';

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

  return (
    <div className="app-shell">
      <aside className="sidebar" aria-label="Основное меню">
        <div className="sidebar__brand">StampService</div>
        <nav className="sidebar__nav">
          <button className="sidebar__item" type="button" disabled>
            <UserRound size={18} />
            Личный кабинет
          </button>
          <button className="sidebar__item sidebar__item--active" type="button">
            <WalletCards size={18} />
            Мой кошелёк
          </button>
          <button className="sidebar__item" type="button" disabled>
            <Workflow size={18} />
            Рабочие бренды
          </button>
        </nav>
      </aside>

      <main className="workspace">
        <header className="workspace__header">
          <div>
            <h1>Мой кошелёк</h1>
            <p>Балансы, доступные награды и код для списания.</p>
          </div>
          <button className="button-secondary" type="button" onClick={auth.signOut}>
            <LogOut size={18} />
            Выйти
          </button>
        </header>

        <WalletPage />
      </main>
    </div>
  );
}
