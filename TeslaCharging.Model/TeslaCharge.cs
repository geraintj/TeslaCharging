using System;

namespace TeslaCharging.Model
{
    public class TeslaCharge
    {
        public string id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Vin { get; set; }
        public string Location { get; set; }
        public string ChargingStatus { get; set; }
    }
}