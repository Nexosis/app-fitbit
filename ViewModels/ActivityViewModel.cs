using System.Collections.Generic;
using System.Linq;
using Nexosis.Api.Client.Model;
using NexosisFitbit.Model;

namespace NexosisFitbit.ViewModels
{
    public class ActivityViewModel
    {
        public ActivityViewModel(IEnumerable<Point> activity, SessionResponse lastSession, IEnumerable<Point> prediction)
        {
            this.Activity = activity.ToList();
            this.LastSession = lastSession;
            this.Prediction = prediction.ToList();
        }
        
        public List<Point> Activity { get; set; }

        public List<Point> Prediction { get; set; }

        public SessionResponse LastSession { get; set; }

        public string SelectedMetric { get; set; }

        public List<Metric> Metrics { get; set; } = Metric.All.ToList();
    }
}