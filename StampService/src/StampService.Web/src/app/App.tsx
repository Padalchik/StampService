import { LogOut, Settings, ShieldCheck, WalletCards, Workflow } from 'lucide-react';
import { useState, type ReactNode } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AdminPage } from '../admin/AdminPage';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import { PhoneLoginPage } from '../auth/PhoneLoginPage';
import { BrandWorkspacePage } from '../brands/BrandWorkspacePage';
import { DesignVariantsPage } from '../design/DesignVariantsPage';
import { ProfilePage } from '../profile/ProfilePage';
import { WalletPage } from '../wallet/WalletPage';
import { navigationLabels } from './navigationLabels';

type ActiveSection = 'profile' | 'wallet' | 'brands' | 'admin';

export function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/design-variants" element={<DesignVariantsPage />} />
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
  const [activeSection, setActiveSection] = useState<ActiveSection>('wallet');
  const pageTitle = getPageTitle(activeSection);
  const pageDescription = getPageDescription(activeSection);

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
          <button
            className={`sidebar__item ${activeSection === 'brands' ? 'sidebar__item--active' : ''}`}
            type="button"
            onClick={() => setActiveSection('brands')}
          >
            <Workflow size={18} />
            {navigationLabels.brandWorkspaces}
          </button>
          <button
            className={`sidebar__item ${activeSection === 'profile' ? 'sidebar__item--active' : ''}`}
            type="button"
            onClick={() => setActiveSection('profile')}
          >
            <Settings size={18} />
            {navigationLabels.accountSettings}
          </button>
          <button
            className={`sidebar__item ${activeSection === 'admin' ? 'sidebar__item--active' : ''}`}
            type="button"
            onClick={() => setActiveSection('admin')}
          >
            <ShieldCheck size={18} />
            {navigationLabels.adminPanel}
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

        {activeSection === 'profile' ? <ProfilePage /> : null}
        {activeSection === 'wallet' ? <WalletPage /> : null}
        {activeSection === 'brands' ? <BrandWorkspacePage /> : null}
        {activeSection === 'admin' ? <AdminPage /> : null}
      </main>
    </div>
  );
}

function getPageTitle(activeSection: ActiveSection): string {
  if (activeSection === 'profile') {
    return navigationLabels.accountSettings;
  }

  if (activeSection === 'brands') {
    return navigationLabels.brandWorkspaces;
  }

  if (activeSection === 'admin') {
    return navigationLabels.adminPanel;
  }

  return navigationLabels.myWallet;
}

function getPageDescription(activeSection: ActiveSection): string {
  if (activeSection === 'profile') {
    return 'Профиль, способы входа и привязка контактов.';
  }

  if (activeSection === 'brands') {
    return 'Выдача и списание метрик, монеток и товаров.';
  }

  if (activeSection === 'admin') {
    return 'Глобальное управление брендами и владельцами.';
  }

  return 'Балансы, доступные награды и код для списания.';
}
