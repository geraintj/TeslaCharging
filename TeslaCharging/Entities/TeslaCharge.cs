using System;

namespace TeslaCharging.Entities
{
    public class TeslaCharge
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Vin { get; set; }
        public string Location { get; set; }
    }
}