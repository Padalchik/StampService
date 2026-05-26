import { FlaskConical, LogOut, Settings, ShieldCheck, WalletCards, Workflow } from 'lucide-react';
import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AdminPage } from '../admin/AdminPage';
import { getAdminAccess } from '../admin/adminApi';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import { PhoneLoginHeroUIPage } from '../auth/PhoneLoginHeroUIPage';
import { PhoneLoginPage } from '../auth/PhoneLoginPage';
import { BrandWorkspacePage } from '../brands/BrandWorkspacePage';
import { getMyBrands, type MyBrandResponse } from '../brands/brandWorkspaceApi';
import { DesignVariantsPage } from '../design/DesignVariantsPage';
import { ProfileHeroUIFormsPage } from '../profile/ProfileHeroUIFormsPage';
import { ProfilePage } from '../profile/ProfilePage';
import { WalletHeroUIPage } from '../wallet/WalletHeroUIPage';
import { WalletPage } from '../wallet/WalletPage';
import { navigationLabels } from './navigationLabels';

type ActiveSection = 'profile' | 'wallet' | 'brands' | 'admin';
type UiVersion = 'default' | 'heroui';

type NavigationAccess = {
  brands: MyBrandResponse[];
  isAdmin: boolean;
};

export function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/design-variants" element={<DesignVariantsPage />} />
          <Route path="/login-heroui" element={<HeroUILoginRoute />} />
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

function HeroUILoginRoute() {
  const auth = useAuth();

  if (auth.isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <PhoneLoginHeroUIPage />;
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
  const [profileVersion, setProfileVersion] = useState<UiVersion>('default');
  const [walletVersion, setWalletVersion] = useState<UiVersion>('default');
  const [navigationAccess, setNavigationAccess] = useState<NavigationAccess>({
    brands: [],
    isAdmin: false
  });
  const singleBrand = navigationAccess.brands.length === 1 ? navigationAccess.brands[0] : null;
  const pageTitle = getPageTitle(activeSection, singleBrand !== null);
  const pageDescription = getPageDescription(activeSection);
  const availableSections = useMemo(
    () => getAvailableSections(navigationAccess),
    [navigationAccess]
  );

  useEffect(() => {
    let isMounted = true;

    async function loadNavigationAccess() {
      const [brandsResult, adminResult] = await Promise.allSettled([
        getMyBrands(),
        getAdminAccess()
      ]);

      if (!isMounted) {
        return;
      }

      setNavigationAccess({
        brands: brandsResult.status === 'fulfilled' ? brandsResult.value.brands : [],
        isAdmin: adminResult.status === 'fulfilled' && adminResult.value
      });
    }

    void loadNavigationAccess();

    return () => {
      isMounted = false;
    };
  }, [auth.token]);

  useEffect(() => {
    if (!availableSections.includes(activeSection)) {
      setActiveSection('wallet');
    }
  }, [activeSection, availableSections]);

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
          {navigationAccess.brands.length > 0 ? (
            <button
              className={`sidebar__item ${activeSection === 'brands' ? 'sidebar__item--active' : ''}`}
              type="button"
              onClick={() => setActiveSection('brands')}
            >
              <Workflow size={18} />
              {singleBrand ? navigationLabels.work : navigationLabels.brandWorkspaces}
            </button>
          ) : null}
          <button
            className={`sidebar__item ${activeSection === 'profile' ? 'sidebar__item--active' : ''}`}
            type="button"
            onClick={() => setActiveSection('profile')}
          >
            <Settings size={18} />
            {navigationLabels.accountSettings}
          </button>
          {navigationAccess.isAdmin ? (
            <button
              className={`sidebar__item ${activeSection === 'admin' ? 'sidebar__item--active' : ''}`}
              type="button"
              onClick={() => setActiveSection('admin')}
            >
              <ShieldCheck size={18} />
              {navigationLabels.adminPanel}
            </button>
          ) : null}
        </nav>
      </aside>

      <main className="workspace">
        <header className="workspace__header">
          <div>
            <h1>{pageTitle}</h1>
            <p>{pageDescription}</p>
          </div>
          <div className="workspace__header-actions">
            {activeSection === 'profile' ? (
              <button
                className="button-secondary"
                type="button"
                onClick={() => setProfileVersion((version) => (version === 'default' ? 'heroui' : 'default'))}
              >
                <FlaskConical size={18} />
                {profileVersion === 'default' ? 'Сменить на HeroUI версию' : 'Сменить на обычную версию'}
              </button>
            ) : null}
            {activeSection === 'wallet' ? (
              <button
                className="button-secondary"
                type="button"
                onClick={() => setWalletVersion((version) => (version === 'default' ? 'heroui' : 'default'))}
              >
                <FlaskConical size={18} />
                {walletVersion === 'default' ? 'Сменить на HeroUI версию' : 'Сменить на обычную версию'}
              </button>
            ) : null}
            <button className="button-secondary" type="button" onClick={auth.signOut}>
              <LogOut size={18} />
              Выйти
            </button>
          </div>
        </header>

        {activeSection === 'profile' && profileVersion === 'default' ? <ProfilePage /> : null}
        {activeSection === 'profile' && profileVersion === 'heroui' ? <ProfileHeroUIFormsPage /> : null}
        {activeSection === 'wallet' && walletVersion === 'default' ? <WalletPage /> : null}
        {activeSection === 'wallet' && walletVersion === 'heroui' ? <WalletHeroUIPage /> : null}
        {activeSection === 'brands' && navigationAccess.brands.length > 0 ? (
          <BrandWorkspacePage
            initialBrands={navigationAccess.brands}
            initialBrandId={singleBrand?.brandId}
          />
        ) : null}
        {activeSection === 'admin' && navigationAccess.isAdmin ? <AdminPage /> : null}
      </main>
    </div>
  );
}

function getAvailableSections(navigationAccess: NavigationAccess): ActiveSection[] {
  const sections: ActiveSection[] = ['wallet', 'profile'];

  if (navigationAccess.brands.length > 0) {
    sections.push('brands');
  }

  if (navigationAccess.isAdmin) {
    sections.push('admin');
  }

  return sections;
}

function getPageTitle(activeSection: ActiveSection, hasSingleBrand: boolean): string {
  if (activeSection === 'profile') {
    return navigationLabels.accountSettings;
  }

  if (activeSection === 'brands') {
    return hasSingleBrand ? navigationLabels.work : navigationLabels.brandWorkspaces;
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
