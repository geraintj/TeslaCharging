export interface ICharge {
    id: string;
    date: Date;
    amount: number;
    vin: string;
    location: string;
    chargeStatus: string;
}