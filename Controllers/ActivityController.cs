using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auth0.Core;
using Fitbit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Nexosis.Api.Client;
using Nexosis.Api.Client.Model;
using NexosisFitbit.Model;
using NexosisFitbit.ViewModels;

namespace NexosisFitbit.Controllers
{
    public class ActivityController : Controller
    {
        private readonly FitbitConnector fitbit;
        private readonly NexosisConnector nexosis;
        private readonly IMemoryCache cache;

        public ActivityController(FitbitConnector fitbit, NexosisConnector nexosis, IMemoryCache cache)
        {
            this.fitbit = fitbit;
            this.nexosis = nexosis;
            this.cache = cache;
        }
        
        [Authorize]
        [HttpPost]
        public async Task <IActionResult> Predict(string id)
        {
            var client = await fitbit.Connect(User);
        
            //fetch all of the activities that we care about 
            var stepsSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Steps, DateTime.Today, DateRangePeriod.Max, "-");
            var distanceSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Distance, DateTime.Today, DateRangePeriod.Max, "-");
            var floorsSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Floors, DateTime.Today, DateRangePeriod.Max, "-");
            var caloriesInSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.CaloriesIn, DateTime.Today, DateRangePeriod.Max, "-");
            var caloriesOutSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.CaloriesOut, DateTime.Today, DateRangePeriod.Max, "-");
            var sleepSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesAsleep, DateTime.Today, DateRangePeriod.Max, "-");
            var fairlyActiveSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesFairlyActive, DateTime.Today, DateRangePeriod.Max, "-");
            var lightlyActiveSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesLightlyActive, DateTime.Today, DateRangePeriod.Max, "-");
            var veryActiveSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesVeryActive, DateTime.Today, DateRangePeriod.Max, "-");
            var waterSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Water, DateTime.Today, DateRangePeriod.Max, "-");
            var weightSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Weight, DateTime.Today, DateRangePeriod.Max, "-");
        
            //join them all into a single dictionary by date
            var dataSetData = from steps in stepsSeries.DataList
                join distance in distanceSeries.DataList on steps.DateTime equals distance.DateTime
                join floors in floorsSeries.DataList on steps.DateTime equals floors.DateTime
                join caloriesIn in caloriesInSeries.DataList on steps.DateTime equals caloriesIn.DateTime
                join caloriesOut in caloriesOutSeries.DataList on steps.DateTime equals caloriesOut.DateTime
                join minutesAsleep in sleepSeries.DataList on steps.DateTime equals minutesAsleep.DateTime
                join minutesFairlyActive in fairlyActiveSeries.DataList on steps.DateTime equals minutesFairlyActive.DateTime
                join minutesLightlyActive in lightlyActiveSeries.DataList on steps.DateTime equals minutesLightlyActive.DateTime
                join minutesVeryActive in veryActiveSeries.DataList on steps.DateTime equals minutesVeryActive.DateTime
                join water in waterSeries.DataList on steps.DateTime equals water.DateTime
                join weight in weightSeries.DataList on steps.DateTime equals weight.DateTime
                select new Dictionary<string, string>
                {
                    ["timeStamp"] = steps.DateTime.ToString("o"),
                    [nameof(steps)] = steps.Value,
                    [nameof(floors)] = floors.Value,
                    [nameof(caloriesIn)] = caloriesIn.Value,
                    [nameof(caloriesOut)] = caloriesOut.Value,
                    [nameof(minutesAsleep)] = minutesAsleep.Value,
                    [nameof(minutesFairlyActive)] = minutesFairlyActive.Value,
                    [nameof(minutesLightlyActive)] = minutesLightlyActive.Value,
                    [nameof(minutesVeryActive)] = minutesVeryActive.Value,
                    [nameof(water)] = water.Value,
                    [nameof(weight)] = weight.Value,
                };
        
            var nexosisClient = nexosis.Connect();
            var fitbitUser = await fitbit.GetFitbitUser(User);
        
            //send that dictionary to Nexosis as a single DataSet
            var request = new DataSetDetail() {Data = dataSetData.ToList()};
            var dataSetName = $"fitbit.{fitbitUser.UserId}"; 
            await nexosisClient.DataSets.Create(DataSet.From(dataSetName, request));
        
            //make sure that we've identified which column in the DataSet is our target (the one we want to predict)
            var sessionRequest = Sessions.Forecast(
                dataSetName,
                new DateTimeOffset(DateTime.Today.AddDays(1)),
                new DateTimeOffset(DateTime.Today.AddDays(31)),
                ResultInterval.Day,
                options: new ForecastSessionRequest()
                {
                    Columns = new Dictionary<string, ColumnMetadata>()
                    {
                        [id] = new ColumnMetadata() {Role = ColumnRole.Target, DataType = ColumnType.Numeric}
                    }
                });
        
            await nexosisClient.Sessions.CreateForecast(sessionRequest);
                    
            return RedirectToAction("Index", new{id=id});
        
        }

        [Authorize]
        public async Task<IActionResult> Index(string id)
        {
        
            if (id == null)
            {
                return RedirectToAction("Index", new {id = "steps"});
            }
            
            TimeSeriesDataList timeSeries = null;
        
            if (!cache.TryGetValue($"{User.Identity.Name}.{id}", out timeSeries))
            {
                var client = await fitbit.Connect(User);
        
                var resourceType = TimeSeriesResourceType.Steps;
                if (!Enum.TryParse(id, true, out resourceType))
                {
                    return RedirectToAction("Index", new {id = "steps"});                    
                }
        
                timeSeries = await client.GetTimeSeriesAsync(resourceType, DateTime.Today,
                    DateRangePeriod.SixMonths, "-");
        
                cache.Set($"{User.Identity.Name}.{id}", timeSeries);
            }
        
            var fitbitUser = await fitbit.GetFitbitUser(User);
        
            var nexosisClient = nexosis.Connect();

            var sessionsForThisActivity =
                (await nexosisClient.Sessions.List(Sessions.Where($"fitbit.{fitbitUser.UserId}")))
                .Items
                .OrderByDescending(o => o.RequestedDate).Where(s => s.TargetColumn == id)
                .ToList();
            
            //look for the most recent completed session targeting the current activity
            var lastSession = sessionsForThisActivity.FirstOrDefault(s => s.Status == Status.Completed)
                              ?? sessionsForThisActivity.FirstOrDefault();
        
            SessionResult result = null;
        
            if (lastSession?.Status == Status.Completed)
            {
                //if we have a session, fetch that session's results
                result = await nexosisClient.Sessions.GetResults(lastSession.SessionId);
            }
        
            var actualPoints = timeSeries.ToPoints().ToList();
            var predictedPoints = result.ToPoints().ToList();
        
            //make sure the two series have the same number of points, just to satisfy nvd3
            predictedPoints = predictedPoints.AlignWith(actualPoints).ToList();
            actualPoints = actualPoints.AlignWith(predictedPoints).ToList();
            
            return View(new ActivityViewModel(actualPoints, lastSession, predictedPoints, id));
        }
    }
}