export interface CommissionResult {
  rentalAmount: number;
  commissionRate: number;
  commissionAmount: number;
  netToCompany: number;
}

const COMMISSION_TIERS = [
  { minAmount: 50000, rate: 0.10 },
  { minAmount: 20000, rate: 0.08 },
  { minAmount: 5000, rate: 0.05 },
  { minAmount: 0, rate: 0.03 },
];

export function calculateCommission(rentalAmount: number): CommissionResult {
  const tier = COMMISSION_TIERS.find((t) => rentalAmount >= t.minAmount)!;
  const commissionAmount = parseFloat((rentalAmount * tier.rate).toFixed(2));
  const netToCompany = parseFloat((rentalAmount - commissionAmount).toFixed(2));

  return {
    rentalAmount,
    commissionRate: tier.rate,
    commissionAmount,
    netToCompany,
  };
}
