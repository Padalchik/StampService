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
  customerCode: string;
  redemptionCode: UserWalletRedemptionCodeResponse;
  brands: UserWalletBrandOverviewResponse[];
};

export function openUserWallet(forceRefreshCode = false): Promise<UserWalletResponse> {
  const query = forceRefreshCode ? '?forceRefreshCode=true' : '';
  return apiRequest<UserWalletResponse>(`/api/wallet/open${query}`, {
    method: 'POST'
  });
}
