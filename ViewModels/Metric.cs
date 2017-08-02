using System.Collections.Generic;

namespace NexosisFitbit.ViewModels {
    public class Metric
    {

        public Metric(string name, string displayName = null)
        {
            this.Name = name;
            this.DisplayName = displayName ?? name;
        }
        
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        
        public static IEnumerable<Metric> All
        {
            get
            {
                yield return new Metric("steps", "Steps");
                yield return new Metric("floors", "Floors");
                yield return new Metric("caloriesIn", "Calories In");
                yield return new Metric("caloriesOut", "Calories Out");
                yield return new Metric("minutesAsleep", "Sleep");
                yield return new Metric("minutesFairlyActive", "Minutes Fairly Active");
                yield return new Metric("minutesLightlyActive", "Minutes Lightly Active");
                yield return new Metric("minutesVeryActive", "Minutes Very Active");
                yield return new Metric("water", "Water");
                yield return new Metric("weight", "Weight");
            }        
        }
    }
}