const GST_RATE = 0.18;
const CGST_RATE = 0.09;
const SGST_RATE = 0.09;

export interface GstBreakdown {
  baseAmount: number;
  cgst: number;
  sgst: number;
  totalGst: number;
  grandTotal: number;
}

export function calculateGst(baseAmount: number): GstBreakdown {
  const cgst = parseFloat((baseAmount * CGST_RATE).toFixed(2));
  const sgst = parseFloat((baseAmount * SGST_RATE).toFixed(2));
  const totalGst = parseFloat((cgst + sgst).toFixed(2));
  const grandTotal = parseFloat((baseAmount + totalGst).toFixed(2));

  return {
    baseAmount,
    cgst,
    sgst,
    totalGst,
    grandTotal,
  };
}

export function calculateRentalWithGst(hourlyRate: number, hours: number): GstBreakdown {
  const baseAmount = parseFloat((hourlyRate * hours).toFixed(2));
  return calculateGst(baseAmount);
}
