//  Authors:  Caren Dymond, Sarah Beukema

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// A disturbance named in the spin-up tables: fire at a severity, or any other type in the disturbance matrix.
    /// </summary>
    public class SpinUpDisturbance
    {
        public readonly string Name;
        public readonly int Severity;       //1 to 5 for fire, 0 otherwise

        public SpinUpDisturbance(string name, int severity)
        {
            Name = name;
            Severity = severity;
        }

        /// <summary>
        /// The name as the disturbance routines take it.
        /// </summary>
        public string DistTypeName
        {
            get { return ":" + Name; }
        }

        public override string ToString()
        {
            return (Severity > 0) ? Name + " " + Severity : Name;
        }

        /// <summary>
        /// The severity-4 fire that ends each cycle of the spin-up without a return interval.
        /// </summary>
        public static readonly SpinUpDisturbance Fire4 = new SpinUpDisturbance("fire", 4);
    }

    /// <summary>
    /// An ecoregion's spin-up when it has a return interval: each cycle grows the site's community for ReturnInterval
    /// years and ends with CycleDisturbance; once the soil pools are stable, one more cycle ends with LastDisturbance
    /// (the stand that preceded the present one), and the present stand then grows to its age.
    /// </summary>
    public class SpinUpReturnInterval
    {
        public readonly int ReturnInterval;
        public readonly SpinUpDisturbance CycleDisturbance;
        public readonly SpinUpDisturbance LastDisturbance;

        public SpinUpReturnInterval(int returnInterval, SpinUpDisturbance cycleDisturbance, SpinUpDisturbance lastDisturbance)
        {
            ReturnInterval = returnInterval;
            CycleDisturbance = cycleDisturbance;
            LastDisturbance = lastDisturbance;
        }

        /// <summary>
        /// The same return interval and cycles, with another last disturbance (from SpinUpLastDisturbanceMap).
        /// </summary>
        public SpinUpReturnInterval WithLastDisturbance(SpinUpDisturbance lastDisturbance)
        {
            return new SpinUpReturnInterval(ReturnInterval, CycleDisturbance, lastDisturbance);
        }
    }
}
