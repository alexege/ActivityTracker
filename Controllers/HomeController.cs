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
            ViewBag.Logged_in_user_id = HttpContext.Session.GetInt32("UserId");
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

            var activities = dbContext.Activities
                            .Include(a => a.Participants)
                            .Include(a => a.Coordinator)
                            .OrderByDescending(d => d.Date)
                            .Where(a =>a.Date > DateTime.Now)
                            .ToList();

            return View("Dashboard", activities);
        }

        //================================== Activities and Misc ==================================
        [HttpGet("activity/new")]
        public IActionResult NewActivity()
        {
            return View();
        }
        
        [HttpPost("activity/new")]
        public IActionResult createActivity(Activity newActivity)
        {
            if(ModelState.IsValid)
            {
                //Create a new Activity
                var current_user = HttpContext.Session.GetInt32("UserId");
                User Coordinator = dbContext.Users.FirstOrDefault(u => u.UserId == current_user);
                // newActivity.Coordinator = dbContext.Users.FirstOrDefault(u => u.UserId == current_user);
                newActivity.Coordinator = Coordinator;
                // newActivity.Coordinator = dbContext.Users.Where(usr => usr.UserId == HttpContext.Session.GetInt32("UserId"));
                dbContext.Activities.Add(newActivity);
                dbContext.SaveChanges();

                var last_added_activity = dbContext.Activities.Last();
                var last_added_activity_id = last_added_activity.ActivityId;
                HttpContext.Session.SetInt32("ActivityId", last_added_activity_id);

                HttpContext.Session.SetInt32("Logged_in_user_id", (int)HttpContext.Session.GetInt32("UserId"));

                var activityId = HttpContext.Session.GetInt32("ActivityId");

                Activity current_activity = dbContext.Activities.FirstOrDefault(a => a.ActivityId == activityId);

                var current_user_id = HttpContext.Session.GetInt32("UserId");
                var current_usr = dbContext.Users.FirstOrDefault(usr => usr.UserId == current_user_id);
                ViewBag.FirstName = current_usr.FirstName;

                return View("ShowActivity", current_activity);
                // return View("Index");
            }
            return View("NewActivity");
        }

        // Join an Activity
        [HttpGet("participate/{ActivityId}")]
        public IActionResult JoinActivity(int ActivityId)
        {
            var user_in_session = (int)HttpContext.Session.GetInt32("UserId");
            Participant newParticipant = new Participant(ActivityId, user_in_session);
            var part = dbContext.Activities.FirstOrDefault(activity => activity.ActivityId == newParticipant.ActivityId);

            dbContext.Participants.Add(newParticipant);
            dbContext.SaveChanges();

            var last_added_participant = dbContext.Participants.Last();
            part.Participants.Add(last_added_participant);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }
        
        // Leave an Activity
        [HttpGet("participate/{ActivityId}/leave")]
        public IActionResult LeaveActivity(int ActivityId)
        {
            var activity_participants = dbContext.Participants.Where(part => part.ActivityId == ActivityId).ToList();

            var user_in_session = HttpContext.Session.GetInt32("UserId");

            var leaving_participant = activity_participants.FirstOrDefault(leave => leave.UserId == user_in_session);
            dbContext.Participants.Remove(leaving_participant);
            dbContext.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        // Delete an Activity
        [HttpGet("participate/{ActivityId}/delete")]
        public IActionResult DeleteActivity(int ActivityId)
        {
            var activity = dbContext.Activities.Include(act => act.Participants)
                                                .FirstOrDefault(act => act.ActivityId == ActivityId);

            var list_of_participants = activity.Participants.ToList();
            foreach(var part in list_of_participants)
            {
                dbContext.Participants.Remove(part);
            }
            dbContext.Activities.Remove(activity);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        [HttpGet("activity/{ActivityId}")]
        public IActionResult ShowActivity(int ActivityId)
        {

            // var current_user_id = HttpContext.Session.GetInt32("UserId");
            // var current_user = dbContext.Users.FirstOrDefault(usr => usr.UserId == current_user_id);
            // ViewBag.FirstName = current_user.FirstName;


            // var last_added_activity = HttpContext.Session.GetInt32("ActivityId");
            // int id = ActivityId;
            // var activity = dbContext.Activities.FirstOrDefault(a => a.ActivityId == last_added_activity);

            // var activities = dbContext.Activities.Include(act => act.Coordinator)
            //                                     .Include(act => act.Participants)
            //                                         .ThenInclude(u => u.User)
            //                                     .FirstOrDefault(a => a.ActivityId == ActivityId);

            // return View(activity);

            Activity current_activity = dbContext.Activities.FirstOrDefault(a => a.ActivityId == ActivityId);
            ViewBag.current_activity = current_activity;
            return View();
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

        // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        // public IActionResult Error()
        // {
        //     return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        // }
    }
}
