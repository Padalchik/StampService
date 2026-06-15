import { apiRequest } from '../api/apiClient';

export type UserWalletRedemptionCodeResponse = {
  code: string;
  expiresAtUtc: string;
};

export type UserBrandCoinProductRewardResponse = {
  productId: string;
  productName: string;
  price: number;
  currentBalance: number;
  missingAmount: number;
  isAvailable: boolean;
};

export type UserBrandMetricRewardResponse = {
  metricDefinitionId: string;
  metricName: string;
  currentBalance: number;
  requiredAmount: number;
  missingAmount: number;
  isAvailable: boolean;
};

export type UserWalletBrandOverviewResponse = {
  brandId: string;
  brandName: string;
  isMetricsEnabled: boolean;
  isCoinsEnabled: boolean;
  isCoinProductRedemptionEnabled: boolean;
  isManualCoinRedemptionEnabled: boolean;
  coinBalance: number;
  availableCoinProducts: UserBrandCoinProductRewardResponse[];
  availableMetrics: UserBrandMetricRewardResponse[];
};

export type UserWalletResponse = {
  userId: string;
  redemptionCode: UserWalletRedemptionCodeResponse;
  brands: UserWalletBrandOverviewResponse[];
};

export type UserWalletBrandRewardItemResponse = {
  itemId: string;
  name: string;
  progressText: string;
  statusText: string;
  isAvailable: boolean;
};

export type UserWalletBrandRewardSectionResponse = {
  kind: string;
  title: string;
  balanceText?: string | null;
  emptyText: string;
  items: UserWalletBrandRewardItemResponse[];
};

export type UserWalletBrandHistoryItemDetailsResponse = {
  sourceType: 'Coin' | 'Metric' | string;
  sourceName: string;
  transactionType: 'Issue' | 'Redeem' | string;
  amount: number;
  amountText: string;
  comment?: string | null;
  hasVisibleComment: boolean;
  actorUserId: string;
  createdAt: string;
};

export type UserWalletBrandHistoryGroupResponse = {
  kind: string;
  title: string;
  emptyText: string;
  items: UserWalletBrandHistoryItemDetailsResponse[];
};

export type UserWalletBrandHistorySectionResponse = {
  title: string;
  emptyText: string;
  groups: UserWalletBrandHistoryGroupResponse[];
};

export type UserWalletBrandDetailsResponse = {
  userId: string;
  brandId: string;
  brandName: string;
  isMetricsEnabled: boolean;
  isCoinsEnabled: boolean;
  isCoinProductRedemptionEnabled: boolean;
  coinBalance: number;
  rewardSections: UserWalletBrandRewardSectionResponse[];
  history: UserWalletBrandHistorySectionResponse;
  hintText: string;
};

export function openUserWallet(forceRefreshCode = false): Promise<UserWalletResponse> {
  const query = forceRefreshCode ? '?forceRefreshCode=true' : '';
  return apiRequest<UserWalletResponse>(`/api/wallet/open${query}`, {
    method: 'POST'
  });
}

export function getCurrentRedemptionCode(forceRefreshCode = false): Promise<UserWalletRedemptionCodeResponse> {
  const query = forceRefreshCode ? '?forceRefreshCode=true' : '';
  return apiRequest<UserWalletRedemptionCodeResponse>(`/api/users/me/redemption-code${query}`, {
    method: 'POST'
  });
}

export function getBrandDetails(brandId: string): Promise<UserWalletBrandDetailsResponse> {
  return apiRequest<UserWalletBrandDetailsResponse>(`/api/wallet/brands/${brandId}/details`);
}
