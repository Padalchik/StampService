import { LogOut, UserRound, WalletCards, Workflow } from 'lucide-react';
import type { ReactNode } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import { PhoneLoginPage } from '../auth/PhoneLoginPage';

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
          <button className="sidebar__item sidebar__item--active" type="button">
            <UserRound size={18} />
            Личный кабинет
          </button>
          <button className="sidebar__item" type="button" disabled>
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
            <h1>Личный кабинет</h1>
            <p>Вход по телефону выполнен. Следующий этап - профиль и смена телефона.</p>
          </div>
          <button className="button-secondary" type="button" onClick={auth.signOut}>
            <LogOut size={18} />
            Выйти
          </button>
        </header>

        <section className="empty-state">
          <h2>Авторизация работает через JWT</h2>
          <p>
            Токен сохранён в auth-слое frontend. Разделы кошелька и рабочих брендов будут
            подключаться следующими этапами по существующим Application-сценариям.
          </p>
          {auth.expiresAt ? (
            <p className="empty-state__meta">
              Сессия действует до {new Intl.DateTimeFormat('ru-RU', {
                dateStyle: 'short',
                timeStyle: 'medium'
              }).format(new Date(auth.expiresAt))}
            </p>
          ) : null}
        </section>
      </main>
    </div>
  );
}
