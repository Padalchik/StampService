# Code map для AI

Используй этот файл как карту входа в код. Он не заменяет чтение конкретных файлов перед изменениями.

## Domain

- User/identity/phone/OTP settings: `src/StampService.Domain/User/*`.
- Brand/BrandCustomer/welcome rewards: `src/StampService.Domain/Brand/*`.
- Roles/permissions: `src/StampService.Domain/Access/*`.
- Stamps/metrics: `src/StampService.Domain/Loyalty/*`.
- Coins/products: `src/StampService.Domain/Coins/*`.
- Customer notifications settings: `src/StampService.Domain/CustomerNotifications/*`.

## Application

- Auth/OTP/SMS: `src/StampService.Application/Auth/*`.
- Phone account/profile identity flows: `src/StampService.Application/Users/*`.
- Brand workspace/customer/staff/reward settings: `src/StampService.Application/Brands/*`.
- Access checks: `src/StampService.Application/Access/*`.
- Metrics ledger/options/issue/redeem: `src/StampService.Application/Metrics/*`.
- Coins ledger/issue/redeem: `src/StampService.Application/Coins/*`.
- Coin products: `src/StampService.Application/CoinProducts/*`.
- Wallet overview/details/open: `src/StampService.Application/Wallet/*`.
- Demo/admin data fill: `src/StampService.Application/Demo/*`.
- Reward digest/customer notifications: `src/StampService.Application/CustomerNotifications/*`.
- Ledger lock port: `src/StampService.Application/Ledger/ILedgerOperationLock.cs`.

## Infrastructure

- EF DbContext/repositories/configurations: `src/StampService.Infrastructure`.
- PostgreSQL ledger advisory locks: `src/StampService.Infrastructure/Services/PostgresLedgerOperationLock.cs`.
- OTP senders: `TelegramAdminPhoneAuthCodeSender`, `SmsAeroPhoneAuthCodeSender`, `CompositePhoneAuthCodeSender`.
- JWT: `src/StampService.Infrastructure/Services/JwtTokenService.cs`.
- Migrations: `src/StampService.Infrastructure/Migrations/*`; не редактировать вручную.

## API endpoints

- Auth:
  - `POST /api/auth/phone/code`
  - `GET /api/auth/phone/sms-settings`
  - `POST /api/auth/phone/verify`
  - `POST /api/auth/telegram`
- Profile/users:
  - `GET /api/users/me`
  - `POST /api/users/me/redemption-code`
  - phone/telegram link/change endpoints under `/api/users/me/*`
- Wallet:
  - `POST /api/wallet/open`
  - `GET /api/wallet/brands/{brandId}/details`
- Brands:
  - `GET /api/brands/mine`
  - `GET /api/brands/{brandId}/workspace`
  - `GET /api/brands/{brandId}/customer-card`
  - `POST /api/brands/{brandId}/customers/by-phone`
  - `GET /api/brands/{brandId}/staff`
  - `PUT /api/brands/{brandId}/reward-settings`
  - `POST /api/brands/{brandId}/staff/by-phone`
  - `DELETE /api/brands/{brandId}/staff/{staffUserId}`
- Metrics:
  - `POST /api/brands/{brandId}/metrics`
  - `GET /api/brands/{brandId}/metrics`
  - `GET /api/brands/{brandId}/metrics/issue-options`
  - `GET /api/brands/{brandId}/metrics/redeem-options`
  - `POST /api/metrics/{metricDefinitionId}/issue-by-phone`
  - `POST /api/metrics/{metricDefinitionId}/redeem`
- Coins/products:
  - `POST /api/brands/{brandId}/coins/issue-by-phone`
  - `POST /api/brands/{brandId}/coins/redeem`
  - coin product CRUD under `/api/brands/{brandId}/coin-products` and `/api/coin-products/*`
  - `POST /api/brands/{brandId}/coin-products/{productId}/purchase`
- Admin:
  - `GET /api/admin/access`
  - `GET /api/admin/brands`
  - `GET /api/admin/audit-logs`
  - `GET/PUT /api/admin/auth-sms-settings`
  - brand owner/demo/reset endpoints under `/api/admin/*`

## Web files

- App shell/navigation: `src/StampService.Web/src/app/App.tsx`, `navigationLabels.ts`.
- Auth: `src/StampService.Web/src/auth/*`.
- Phone input: `src/StampService.Web/src/components/RuPhoneInput.tsx`.
- API client/errors: `src/StampService.Web/src/api/*`.
- Wallet: `src/StampService.Web/src/wallet/*`.
- Brand workspace: `src/StampService.Web/src/brands/*`.
- Profile: `src/StampService.Web/src/profile/*`.
- Admin: `src/StampService.Web/src/admin/*`.
- Styles/responsive layout: `src/StampService.Web/src/styles.css`.

## Telegram bot

- Host/DI: `src/StampService.TelegramBot/Program.cs`.
- Main menu: `src/StampService.TelegramBot/Features/MainMenu`.
- Profile phone/Telegram onboarding: `Features/Profile`.
- Brand workspace/settings: `Features/Brands`.
- Staff: `Features/Staff`.
- Issue/redeem metrics: `Features/IssueMetric`, `Features/RedeemMetric`, `Features/Metrics`.
- Coins/products: `Features/Coins`, `Features/CoinProducts`.
- Wallet: `Features/Wallet`.
- Customer balances: `Features/CustomerBalances`.
- Admin/demo/reward digest: `Features/Admin`.
- Notifications: `Common/Notifications`.

## Tests to inspect when allowed

- Domain invariants: `Tests/StampService.DomainTests`.
- Application flows: `Tests/StampService.ApplicationTests`.
- API error mapping: `Tests/StampService.APITests`.

Не запускать tests/build без явного разрешения владельца.
