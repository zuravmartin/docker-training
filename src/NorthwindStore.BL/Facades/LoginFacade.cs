using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Extensions.Logging;
using NorthwindStore.BL.DTO;
using Riganti.Utils.Infrastructure.Services.Facades;

namespace NorthwindStore.BL.Facades
{
    public class LoginFacade : FacadeBase
    {
        private readonly ILogger<LoginFacade> logger;
        private readonly TelemetryClient telemetryClient;

        public LoginFacade(ILogger<LoginFacade> logger, TelemetryClient telemetryClient)
        {
            this.logger = logger;
            this.telemetryClient = telemetryClient;
        }

        public ClaimsPrincipal SignIn(LoginDTO loginData)
        {
            // TODO: incorporate ASP.NET Identity

            if (loginData.UserName == "admin" && loginData.Password == "admin")
            {
                logger.LogInformation($"Successful login {loginData.UserName}");

                return new ClaimsPrincipal(
                    new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "admin")
                    }, 
                    "Cookie")
                );
            }

            telemetryClient.TrackEvent("Unsuccessful login", new Dictionary<string, string>() { { "userName", loginData.UserName } });
            logger.LogWarning($"Unsuccessful login attempt {loginData.UserName}");
            return null;
        }
 
    }
}
