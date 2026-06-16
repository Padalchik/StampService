import { apiRequest } from '../api/apiClient';
import { type UserWalletBrandDetailsResponse } from '../wallet/walletApi';

export type MyBrandResponse = {
  brandId: string;
  brandName: string;
  roleSystemName: string;
};

export type MyBrandsResponse = {
  userId: string;
  brands: MyBrandResponse[];
};

export type BrandWorkspaceResponse = {
  brandId: string;
  brandName: string;
  roleSystemName: string;
  isMetricsEnabled: boolean;
  isCoinsEnabled: boolean;
  isCoinProductRedemptionEnabled: boolean;
  isManualCoinRedemptionEnabled: boolean;
  canIssue: boolean;
  canRedeem: boolean;
  canViewBalances: boolean;
  canManageBrand: boolean;
  canManageMetrics: boolean;
  canManageStaff: boolean;
};

export type BrandStaffResponse = {
  userId: string;
  userName: string;
  phoneNumber?: string | null;
  membershipCreatedAt: string;
};

export type AddBrandStaffByPhoneResponse = {
  brandId: string;
  userId: string;
  userName: string;
  phoneNumber: string;
  membershipId: string;
  membershipCreatedAt: string;
};

export type RemoveBrandStaffResponse = {
  brandId: string;
  userId: string;
  userName: string;
  phoneNumber?: string | null;
};

export type UpdateBrandResponse = {
  brandId: string;
  brandName: string;
  isMetricsEnabled: boolean;
  isCoinsEnabled: boolean;
  isCoinProductRedemptionEnabled: boolean;
  isManualCoinRedemptionEnabled: boolean;
  updatedAt?: string | null;
};

export type MetricResponse = {
  id: string;
  brandId: string;
  name: string;
  redemptionAmount: number;
  isActive: boolean;
  createdAt: string;
};

export type IssueMetricResponse = {
  transactionId: string;
  balanceId: string;
  brandId: string;
  metricDefinitionId: string;
  userId: string;
  transactionType: string;
  amount: number;
  balanceValue: number;
  createdAt: string;
};

export type RedeemMetricOptionResponse = {
  metricDefinitionId: string;
  metricName: string;
  currentBalance: number;
  requiredAmount: number;
  canRedeem: boolean;
};

export type RedeemMetricOptionsResponse = {
  customerUserId: string;
  customerName: string;
  redemptionCode: string;
  metrics: RedeemMetricOptionResponse[];
};

export type RedeemMetricResponse = {
  transactionId: string;
  balanceId: string;
  brandId: string;
  metricDefinitionId: string;
  userId: string;
  transactionType: string;
  amount: number;
  balanceValue: number;
  createdAt: string;
};

export type CoinOperationResponse = {
  transactionId: string;
  walletId: string;
  brandId: string;
  userId: string;
  userName: string;
  transactionType: string;
  amount: number;
  balanceValue: number;
  createdAt: string;
};

export type CoinProductResponse = {
  id: string;
  brandId: string;
  name: string;
  price: number;
  isActive: boolean;
  createdAt: string;
};

export type CoinProductPurchaseOptionResponse = {
  productId: string;
  productName: string;
  price: number;
  currentBalance: number;
  canPurchase: boolean;
};

export type CoinProductPurchaseOptionsResponse = {
  customerUserId: string;
  customerName: string;
  redemptionCode: string;
  products: CoinProductPurchaseOptionResponse[];
};

export type BrandCustomerMetricBalanceResponse = {
  metricDefinitionId: string;
  metricName: string;
  value: number;
  isActive: boolean;
};

export type BrandCustomerBalancesResponse = {
  brandId: string;
  customerUserId: string;
  customerName: string;
  customerPhoneNumber: string;
  coinBalanceValue: number;
  balances: BrandCustomerMetricBalanceResponse[];
};

export type BrandCustomerCardResponse = {
  brandId: string;
  customerUserId: string;
  customerName: string;
  customerPhoneNumber: string;
  details: UserWalletBrandDetailsResponse;
};

export type BrandCustomerCardLookupResponse = {
  found: boolean;
  card?: BrandCustomerCardResponse | null;
};

export function getMyBrands(): Promise<MyBrandsResponse> {
  return apiRequest<MyBrandsResponse>('/api/brands/mine');
}

export function getBrandWorkspace(brandId: string): Promise<BrandWorkspaceResponse> {
  return apiRequest<BrandWorkspaceResponse>(`/api/brands/${brandId}/workspace`);
}

export function getBrandCustomerCard(
  brandId: string,
  customerPhoneNumber: string
): Promise<BrandCustomerCardLookupResponse> {
  return apiRequest<BrandCustomerCardLookupResponse>(
    `/api/brands/${brandId}/customer-card?customerPhoneNumber=${encodeURIComponent(customerPhoneNumber)}`
  );
}

export function createBrandCustomerByPhone(
  brandId: string,
  phoneNumber: string
): Promise<BrandCustomerCardResponse> {
  return apiRequest<BrandCustomerCardResponse>(`/api/brands/${brandId}/customers/by-phone`, {
    method: 'POST',
    body: { phoneNumber }
  });
}

export function getBrandStaff(brandId: string): Promise<BrandStaffResponse[]> {
  return apiRequest<BrandStaffResponse[]>(`/api/brands/${brandId}/staff`);
}

export function addBrandStaffByPhone(
  brandId: string,
  phoneNumber: string
): Promise<AddBrandStaffByPhoneResponse> {
  return apiRequest<AddBrandStaffByPhoneResponse>(`/api/brands/${brandId}/staff/by-phone`, {
    method: 'POST',
    body: { phoneNumber }
  });
}

export function removeBrandStaff(
  brandId: string,
  staffUserId: string
): Promise<RemoveBrandStaffResponse> {
  return apiRequest<RemoveBrandStaffResponse>(`/api/brands/${brandId}/staff/${staffUserId}`, {
    method: 'DELETE'
  });
}

export function updateBrandRewardSettings(
  brandId: string,
  request: {
    isMetricsEnabled: boolean;
    isCoinsEnabled: boolean;
    isCoinProductRedemptionEnabled: boolean;
    isManualCoinRedemptionEnabled: boolean;
  }
): Promise<UpdateBrandResponse> {
  return apiRequest<UpdateBrandResponse>(`/api/brands/${brandId}/reward-settings`, {
    method: 'PUT',
    body: request
  });
}

export function getIssueMetricOptions(brandId: string): Promise<MetricResponse[]> {
  return apiRequest<MetricResponse[]>(`/api/brands/${brandId}/metrics/issue-options`);
}

export function getManageMetrics(brandId: string): Promise<MetricResponse[]> {
  return apiRequest<MetricResponse[]>(`/api/brands/${brandId}/metrics`);
}

export function createMetric(
  brandId: string,
  request: { name: string; redemptionAmount: number }
): Promise<MetricResponse> {
  return apiRequest<MetricResponse>(`/api/brands/${brandId}/metrics`, {
    method: 'POST',
    body: request
  });
}

export function updateMetric(
  metricDefinitionId: string,
  request: { name: string; redemptionAmount: number }
): Promise<MetricResponse> {
  return apiRequest<MetricResponse>(`/api/metrics/${metricDefinitionId}`, {
    method: 'PUT',
    body: request
  });
}

export function getRedeemMetricOptions(
  brandId: string,
  redemptionCode: string
): Promise<RedeemMetricOptionsResponse> {
  return apiRequest<RedeemMetricOptionsResponse>(
    `/api/brands/${brandId}/metrics/redeem-options?redemptionCode=${encodeURIComponent(redemptionCode)}`
  );
}

export function getCustomerBalances(
  brandId: string,
  customerPhoneNumber: string
): Promise<BrandCustomerBalancesResponse> {
  return apiRequest<BrandCustomerBalancesResponse>(
    `/api/brands/${brandId}/customer-balances?customerPhoneNumber=${encodeURIComponent(customerPhoneNumber)}`
  );
}

export function issueMetricByPhone(
  metricDefinitionId: string,
  request: { phoneNumber: string; amount: number; comment?: string }
): Promise<IssueMetricResponse> {
  return apiRequest<IssueMetricResponse>(`/api/metrics/${metricDefinitionId}/issue-by-phone`, {
    method: 'POST',
    body: request
  });
}

export function redeemMetric(
  metricDefinitionId: string,
  request: { redemptionCode: string; comment: string }
): Promise<RedeemMetricResponse> {
  return apiRequest<RedeemMetricResponse>(`/api/metrics/${metricDefinitionId}/redeem`, {
    method: 'POST',
    body: request
  });
}

export function issueCoinsByPhone(
  brandId: string,
  request: { phoneNumber: string; amount: number; comment?: string }
): Promise<CoinOperationResponse> {
  return apiRequest<CoinOperationResponse>(`/api/brands/${brandId}/coins/issue-by-phone`, {
    method: 'POST',
    body: request
  });
}

export function redeemCoins(
  brandId: string,
  request: { redemptionCode: string; amount: number; comment: string }
): Promise<CoinOperationResponse> {
  return apiRequest<CoinOperationResponse>(`/api/brands/${brandId}/coins/redeem`, {
    method: 'POST',
    body: request
  });
}

export function getManageCoinProducts(brandId: string): Promise<CoinProductResponse[]> {
  return apiRequest<CoinProductResponse[]>(`/api/brands/${brandId}/coin-products`);
}

export function createCoinProduct(
  brandId: string,
  request: { name: string; price: number }
): Promise<CoinProductResponse> {
  return apiRequest<CoinProductResponse>(`/api/brands/${brandId}/coin-products`, {
    method: 'POST',
    body: request
  });
}

export function updateCoinProduct(
  productId: string,
  request: { name: string; price: number }
): Promise<CoinProductResponse> {
  return apiRequest<CoinProductResponse>(`/api/coin-products/${productId}`, {
    method: 'PUT',
    body: request
  });
}

export function deleteCoinProduct(productId: string): Promise<CoinProductResponse> {
  return apiRequest<CoinProductResponse>(`/api/coin-products/${productId}`, {
    method: 'DELETE'
  });
}

export function getCoinProductPurchaseOptions(
  brandId: string,
  redemptionCode: string
): Promise<CoinProductPurchaseOptionsResponse> {
  return apiRequest<CoinProductPurchaseOptionsResponse>(
    `/api/brands/${brandId}/coin-products/purchase-options?redemptionCode=${encodeURIComponent(redemptionCode)}`
  );
}

export function purchaseCoinProduct(
  brandId: string,
  productId: string,
  redemptionCode: string
): Promise<CoinOperationResponse> {
  return apiRequest<CoinOperationResponse>(`/api/brands/${brandId}/coin-products/${productId}/purchase`, {
    method: 'POST',
    body: { redemptionCode }
  });
}
