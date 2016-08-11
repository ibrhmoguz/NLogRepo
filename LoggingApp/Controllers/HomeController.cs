using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Web;
using System.Web.Mvc;
using LoggingApp.Models;
using Newtonsoft.Json;
using NLog;

namespace LoggingApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public void Log()
        {
            const string clientName = "ABC Company";
            const string userName = "User1";
            Session["ClientName"] = clientName;
            Session["Username"] = userName;
            var customer = new Customer()
            {
                Age = 30,
                CreatedTime = DateTime.Now,
                Name = "Ibrahim",
                Surname = "Oğuz"
            };
            
            var logger = LogManager.GetCurrentClassLogger();
            GlobalDiagnosticsContext.Set("ClientName", clientName);
            GlobalDiagnosticsContext.Set("Username", userName);
            logger.Debug("Debug level log - client: {0}, message: {1}", clientName, JsonConvert.SerializeObject(customer));
            //logger.Fatal(new Exception("deneme 1 2 3 "));
            
            throw new InvalidCredentialException("Test Message");
        }
    }
}
