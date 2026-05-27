import { LogOut, Settings, ShieldCheck, WalletCards, Workflow, type LucideIcon } from 'lucide-react';
import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AdminPage } from '../admin/AdminPage';
import { getAdminAccess } from '../admin/adminApi';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import { PhoneLoginPage } from '../auth/PhoneLoginPage';
import { BrandWorkspacePage } from '../brands/BrandWorkspacePage';
import { getMyBrands, type MyBrandResponse } from '../brands/brandWorkspaceApi';
import { DesignVariantsPage } from '../design/DesignVariantsPage';
import { ProfilePage } from '../profile/ProfilePage';
import { WalletPage } from '../wallet/WalletPage';
import { navigationLabels } from './navigationLabels';

type ActiveSection = 'profile' | 'wallet' | 'brands' | 'admin';

type NavigationAccess = {
  brands: MyBrandResponse[];
  isAdmin: boolean;
};

type NavigationItem = {
  section: ActiveSection;
  desktopLabel: string;
  mobileLabel: string;
  icon: LucideIcon;
};

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
  const [navigationAccess, setNavigationAccess] = useState<NavigationAccess>({
    brands: [],
    isAdmin: false
  });
  const singleBrand = navigationAccess.brands.length === 1 ? navigationAccess.brands[0] : null;
  const pageTitle = getPageTitle(activeSection, singleBrand !== null);
  const pageDescription = getPageDescription(activeSection);
  const navigationItems = useMemo(
    () => getNavigationItems(navigationAccess),
    [navigationAccess]
  );
  const availableSections = useMemo(
    () => navigationItems.map((item) => item.section),
    [navigationItems]
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
    <div className="app-shell app-shell--with-bottom-nav">
      <DesktopNavigation
        activeSection={activeSection}
        items={navigationItems}
        onNavigate={setActiveSection}
      />

      <main className="workspace">
        <header className="workspace__header">
          <div>
            <h1>{pageTitle}</h1>
            {pageDescription ? <p>{pageDescription}</p> : null}
          </div>
          <div className="workspace__header-actions">
            <button className="button-secondary" type="button" onClick={auth.signOut}>
              <LogOut size={18} />
              Выйти
            </button>
          </div>
        </header>

        {activeSection === 'profile' ? <ProfilePage /> : null}
        {activeSection === 'wallet' ? <WalletPage /> : null}
        {activeSection === 'brands' && navigationAccess.brands.length > 0 ? (
          <BrandWorkspacePage
            initialBrands={navigationAccess.brands}
            initialBrandId={singleBrand?.brandId}
          />
        ) : null}
        {activeSection === 'admin' && navigationAccess.isAdmin ? <AdminPage /> : null}
      </main>

      <MobileBottomNavigation
        activeSection={activeSection}
        items={navigationItems}
        onNavigate={setActiveSection}
      />
    </div>
  );
}

function DesktopNavigation({
  activeSection,
  items,
  onNavigate
}: {
  activeSection: ActiveSection;
  items: NavigationItem[];
  onNavigate: (section: ActiveSection) => void;
}) {
  return (
    <aside className="sidebar" aria-label="Основное меню">
      <div className="sidebar__brand">StampService</div>
      <nav className="sidebar__nav">
        {items.map((item) => {
          const Icon = item.icon;
          const isActive = activeSection === item.section;

          return (
            <button
              className={`sidebar__item ${isActive ? 'sidebar__item--active' : ''}`}
              type="button"
              key={item.section}
              onClick={() => onNavigate(item.section)}
            >
              <Icon size={18} aria-hidden="true" />
              {item.desktopLabel}
            </button>
          );
        })}
      </nav>
    </aside>
  );
}

function MobileBottomNavigation({
  activeSection,
  items,
  onNavigate
}: {
  activeSection: ActiveSection;
  items: NavigationItem[];
  onNavigate: (section: ActiveSection) => void;
}) {
  return (
    <nav className="mobile-bottom-nav" aria-label="Основная навигация">
      {items.map((item) => {
        const Icon = item.icon;
        const isActive = activeSection === item.section;

        return (
          <button
            className={`mobile-bottom-nav__item ${isActive ? 'mobile-bottom-nav__item--active' : ''}`}
            type="button"
            key={item.section}
            aria-current={isActive ? 'page' : undefined}
            onClick={() => onNavigate(item.section)}
          >
            <Icon size={20} aria-hidden="true" />
            <span>{item.mobileLabel}</span>
          </button>
        );
      })}
    </nav>
  );
}

function getNavigationItems(navigationAccess: NavigationAccess): NavigationItem[] {
  const items: NavigationItem[] = [
    {
      section: 'wallet',
      desktopLabel: navigationLabels.myWallet,
      mobileLabel: navigationLabels.wallet,
      icon: WalletCards
    }
  ];

  if (navigationAccess.brands.length > 0) {
    const hasSingleBrand = navigationAccess.brands.length === 1;

    items.push({
      section: 'brands',
      desktopLabel: hasSingleBrand ? navigationLabels.work : navigationLabels.brandWorkspaces,
      mobileLabel: hasSingleBrand ? navigationLabels.work : navigationLabels.brands,
      icon: Workflow
    });
  }

  if (navigationAccess.isAdmin) {
    items.push({
      section: 'admin',
      desktopLabel: navigationLabels.adminPanel,
      mobileLabel: navigationLabels.adminPanel,
      icon: ShieldCheck
    });
  }

  items.push({
    section: 'profile',
    desktopLabel: navigationLabels.accountSettings,
    mobileLabel: navigationLabels.account,
    icon: Settings
  });

  return items;
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

  return '';
}
