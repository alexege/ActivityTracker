using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CSharpBelt.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace CSharpBelt.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;
		// here we can "inject" our context service into the constructor
		public HomeController(MyContext context)
		{
			dbContext = context;
		}

        public IActionResult Index()
        {
            return View();
        }

        // Let a new user login
        [HttpPost("Login")]
        public IActionResult Login(LogUser logUser)
        {
            var found_user = dbContext.Users.FirstOrDefault(user => user.Email == logUser.LogEmail);

            if(found_user == null)
            {
                ModelState.AddModelError("LogEmail", "Incorrect Email or Password");
                return View("Index");
            }

            PasswordHasher<LogUser> Hasher = new PasswordHasher<LogUser>();
            var user_verified = Hasher.VerifyHashedPassword(logUser, found_user.Password, logUser.LogPassword);

            if(user_verified == 0)
            {
                ModelState.AddModelError("LogEmail", "Email already in use. Please use a new one");
                return View("Index");
            }

            // var current_user = dbContext.Users.Last().UserId;

            HttpContext.Session.SetInt32("UserId", dbContext.Users.Last().UserId);
            
            return RedirectToAction("Dashboard");
        }

        //Register a new user
        [HttpPost("Register")]
        public IActionResult Register(User newUser)
        {
            if(ModelState.IsValid)
            {
                bool notUnique = dbContext.Users.Any(a => a.Email == newUser.Email);

                if(notUnique)
                {
                    ModelState.AddModelError("Email", "Email already in use. Please use a new one");
                    return View("Index");
                }

                PasswordHasher<User> hasher = new PasswordHasher<User>();
                string hash = hasher.HashPassword(newUser, newUser.Password);
                newUser.Password = hash;

                dbContext.Users.Add(newUser);
                dbContext.SaveChanges();

                var last_added_User = dbContext.Users.Last().UserId;
                HttpContext.Session.SetInt32("UserId", last_added_User);
            
                return RedirectToAction("Dashboard");
            }
        return View("Index");
        }

        //Navigate to the Dashboard on successful Login/Registration
        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            // checked to see if user is in session or not. If not, redirec to index.
            if(HttpContext.Session.GetInt32("UserId") == null){
                return View("Index");
            }


            int? UserId = HttpContext.Session.GetInt32("UserId");
            if(UserId == null)
            {
                return View("Index");
            }

            var current_user = dbContext.Users.First(usr => usr.UserId == UserId);
            ViewBag.FirstName = current_user.FirstName;

            ViewBag.Logged_in_user_id = HttpContext.Session.GetInt32("UserId");

            // var weddings = dbContext.Weddings
            //             .Include(w => w.Creator)
            //             .Include(w => w.RSVPs)
            //                 .ThenInclude(r => r.Guest)
            //             .ToList();
            // return View("Dashboard", weddings);

            return View("Dashboard");
        }

        //Log a user out of session
        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            //Destroy Session
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
