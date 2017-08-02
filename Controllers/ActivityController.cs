using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auth0.Core;
using Fitbit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
        public async Task <IActionResult> Predict()
        {
            var client = await fitbit.Connect(User);

            var stepsSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Steps, DateTime.Today, DateRangePeriod.Max, "-");
            var distanceSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Distance, DateTime.Today, DateRangePeriod.Max, "-");
            var floorsSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Floors, DateTime.Today, DateRangePeriod.Max, "-");
            //var activityCaloriesSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.ActivityCalories, DateTime.Today, DateRangePeriod.Max, "-");
            var caloriesInSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.CaloriesIn, DateTime.Today, DateRangePeriod.Max, "-");
            var caloriesOutSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.CaloriesOut, DateTime.Today, DateRangePeriod.Max, "-");
            var sleepSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesAsleep, DateTime.Today, DateRangePeriod.Max, "-");
            var fairlyActiveSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesFairlyActive, DateTime.Today, DateRangePeriod.Max, "-");
            var lightlyActiveSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesLightlyActive, DateTime.Today, DateRangePeriod.Max, "-");
            var veryActiveSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesVeryActive, DateTime.Today, DateRangePeriod.Max, "-");
            var waterSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Water, DateTime.Today, DateRangePeriod.Max, "-");
            var weightSeries = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Weight, DateTime.Today, DateRangePeriod.Max, "-");

            var dataSetData = from steps in stepsSeries.DataList
                join distance in distanceSeries.DataList on steps.DateTime equals distance.DateTime
                join floors in floorsSeries.DataList on steps.DateTime equals floors.DateTime
                //join activityCalories in activityCaloriesSeries.DataList on steps.DateTime equals activityCalories.DateTime
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
                    //[nameof(activityCalories)] = activityCalories.Value,
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

            var request = new DataSetDetail() {Data = dataSetData.ToList()};

            var dataSetName = $"fitbit.{fitbitUser.UserId}"; 
            await nexosisClient.DataSets.Create(dataSetName, request);

            var columns = request.Data.SelectMany(r => r.Keys).Distinct().Except(new[] {"timeStamp", "steps"}).Select(
                    c => new KeyValuePair<string, ColumnMetadata>(c,
                        new ColumnMetadata() {DataType = ColumnType.Numeric, Role = ColumnRole.None}))
                .ToDictionary(k => k.Key, v => v.Value);
            
            columns.Add("steps", new ColumnMetadata() {DataType = ColumnType.Numeric, Role = ColumnRole.Target});

            var sessionRequest = new SessionDetail()
            {
                DataSetName = dataSetName,
                Columns =  columns
            };

            await nexosisClient.Sessions.CreateForecast(sessionRequest,
                new DateTimeOffset(DateTime.Today), new DateTimeOffset(DateTime.Today.AddDays(30)), ResultInterval.Day);
                    
            return RedirectToAction("Index");

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

            var lastSession = (await nexosisClient.Sessions.List($"fitbit.{fitbitUser.UserId}"))
                .OrderByDescending(o=>o.RequestedDate).FirstOrDefault(s => s.TargetColumn == id);

            SessionResult result = null;

            if (lastSession?.Status == Status.Completed)
            {
                result = await nexosisClient.Sessions.GetResults(lastSession.SessionId);
            }
            
            return View(new ActivityViewModel(timeSeries.ToPoints(), lastSession, result.ToPoints(), id));
        }
    }
}