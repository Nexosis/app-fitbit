using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fitbit.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using NexosisFitbit.Model;

namespace NexosisFitbit.Controllers
{
    public class HomeController : Controller
    {
        private FitbitConnector fitbit;

        public HomeController(FitbitConnector fitbit)
        {
            this.fitbit = fitbit;
        }

        public async Task<IActionResult> Index()
        {

            if (User.Identity.IsAuthenticated)
            {
                if (await fitbit.CanConnect(User))
                {
                    return RedirectToAction("Activity");
                }
                else
                {
                    await HttpContext.Authentication.SignOutAsync("Auth0", new AuthenticationProperties
                    {
                        RedirectUri = Url.Action("Index", "Home")
                    });
                    await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
                
            }

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Activity()
        {
            var client = await fitbit.Connect(User);
            


            var timeSeries = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.Steps, DateTime.Today,
                DateRangePeriod.ThreeMonths, "-");

            return View(timeSeries.ToPoints());
        }

        [Authorize]
        [HttpPost]
        public async Task <IActionResult> Predict()
        {
            var client = await fitbit.Connect(User);

            var steps = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.Steps, DateTime.Today, DateRangePeriod.Max, "-");
            var distance = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.Distance, DateTime.Today, DateRangePeriod.Max, "-");
            var floors = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.Floors, DateTime.Today, DateRangePeriod.Max, "-");
            var activityCalories = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.ActivityCalories, DateTime.Today, DateRangePeriod.Max, "-");
            var caloriesIn = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.CaloriesIn, DateTime.Today, DateRangePeriod.Max, "-");
            var caloriesOut = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.CaloriesOut, DateTime.Today, DateRangePeriod.Max, "-");
            var sleep = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.MinutesAsleep, DateTime.Today, DateRangePeriod.Max, "-");
            var fairlyActive = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.MinutesFairlyActive, DateTime.Today, DateRangePeriod.Max, "-");
            var lightlyActive = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.MinutesLightlyActive, DateTime.Today, DateRangePeriod.Max, "-");
            var veryActive = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.MinutesVeryActive, DateTime.Today, DateRangePeriod.Max, "-");
            var waiter = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.Water, DateTime.Today, DateRangePeriod.Max, "-");
            var weight = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.Weight, DateTime.Today, DateRangePeriod.Max, "-");

            return View("Activity");

        }
        
        


        public IActionResult Error()
        {
            return View();
        }
    }
}
