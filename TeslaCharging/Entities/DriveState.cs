using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace TeslaCharging.Entities
{
    public class DriveState
    {
        public long Timestamp { get; set; }
        public int Heading { get; set; }
        public double? Speed { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Power { get; set; }
    }
}
