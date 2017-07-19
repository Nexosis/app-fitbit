using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fitbit.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nexosis.Api.Client.Model;
using NexosisFitbit.Model;

namespace NexosisFitbit.Controllers
{
    public class HomeController : Controller
    {
        private readonly FitbitConnector fitbit;

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
                    return RedirectToAction("Index", "Activity");
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

        public IActionResult Error()
        {
            return View();
        }
    }
}
