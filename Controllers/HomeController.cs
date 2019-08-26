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
            // Look to see if user exists in database
            var found_user = dbContext.Users.FirstOrDefault(user => user.Email == logUser.LogEmail);

            // If no user found via that email address, display error and redirect back to index page.
            if(found_user == null)
            {
                ModelState.AddModelError("LogEmail", "Incorrect Email or Password");
                return View("Index");
            }

            //If a user is found, Verify their password to the hashed password stored in the database.
            PasswordHasher<LogUser> Hasher = new PasswordHasher<LogUser>();
            var user_verified = Hasher.VerifyHashedPassword(logUser, found_user.Password, logUser.LogPassword);

            //If VerifyHashedPassword returns a 0, Passwords didn't match. Return user to Index.
            if(user_verified == 0)
            {
                ModelState.AddModelError("LogEmail", "Incorrect Email or Password");
                return View("Index");
            }

            //Store logged in user's id into session.
            HttpContext.Session.SetInt32("UserId", found_user.UserId);

            //Store logged in user's id into ViewBag.
            ViewBag.Logged_in_user_id = found_user.UserId;

            return RedirectToAction("Dashboard");
        }

        //Register a new user
        [HttpPost("Register")]
        public IActionResult Register(User newUser)
        {
            //If ModelState contains no errors
            if(ModelState.IsValid)
            {
                //Check to see if email address already exists in database
                bool notUnique = dbContext.Users.Any(a => a.Email == newUser.Email);

                //If email already taken,display error and redirect to index.
                if(notUnique)
                {
                    ModelState.AddModelError("Email", "Email already in use. Please use a new one.");
                    return View("Index");
                }

                //If unique password, hash the new user's password
                PasswordHasher<User> hasher = new PasswordHasher<User>();
                string hash = hasher.HashPassword(newUser, newUser.Password);
                newUser.Password = hash;

                dbContext.Users.Add(newUser);
                dbContext.SaveChanges();

                //Store new user's id into session
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
            //Checked to see if user is in session or not. If not, redirec to index.
            if(HttpContext.Session.GetInt32("UserId") == null){
                return View("Index");
            }

            //Get user id from session
            int? UserId = HttpContext.Session.GetInt32("UserId");

            //If no user in session, redirect to index
            if(UserId == null)
            {
                return View("Index");
            }

            //Place current logged in user's name in Viewbag.FirstName
            var current_user = dbContext.Users.First(usr => usr.UserId == UserId);
            ViewBag.FirstName = current_user.FirstName;

            //Place current logged in user's id in Viewbag.Logged_in_user_id
            ViewBag.Logged_in_user_id = HttpContext.Session.GetInt32("UserId");

            //Get a list of activities meeting the following criteria
            var activities = dbContext.Activities
                            .Include(a => a.Participants)
                            .Include(a => a.Coordinator)
                            .OrderByDescending(d => d.Date)
                            // .Where(a =>a.Date >= DateTime.Now)
                            // .Where(t => t.Time >= DateTime.Now)
                            .ToList();

            //Render the Dashboard view and push the list of activities
            return View("Dashboard", activities);
        }

        //Render NewActivity page
        [HttpGet("activity/new")]
        public IActionResult NewActivity()
        {
            return View();
        }

        //Create a new Activity
        [HttpPost("activity/new")]
        public IActionResult createActivity(Activity newActivity)
        {
            if(ModelState.IsValid)
            {
                //Grab current user id from session
                var current_user = HttpContext.Session.GetInt32("UserId");
                User Coordinator = dbContext.Users.FirstOrDefault(u => u.UserId == current_user);

                newActivity.UserId = (int)current_user;
                dbContext.Activities.Add(newActivity);
                dbContext.SaveChanges();

                //Store last added activity id in session
                var last_added_activity_id = dbContext.Activities.Last().ActivityId;
                HttpContext.Session.SetInt32("ActivityId", last_added_activity_id);

                //Redirect to ShowActivity passing the new ActivityId
                return RedirectToAction("ShowActivity", new { ActivityId = newActivity.ActivityId});
            }
            return View("NewActivity");
        }

        //Join an Activity
        [HttpGet("participate/{ActivityId}")]
        public IActionResult JoinActivity(int ActivityId)
        {
            //Grab UserId from session
            var user_in_session = (int)HttpContext.Session.GetInt32("UserId");

            //Add current logged in user to list of participants for specified activity
            Participant newParticipant = new Participant(ActivityId, user_in_session);

            //Add the new participant
            dbContext.Participants.Add(newParticipant);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }
        
        //Leave an Activity
        [HttpGet("participate/{ActivityId}/leave")]
        public IActionResult LeaveActivity(int ActivityId)
        {
            //Grab a list of participants for specific Activity
            var activity_participants = dbContext.Participants.Where(part => part.ActivityId == ActivityId).ToList();

            //Grab current logged in UserId
            var user_in_session = HttpContext.Session.GetInt32("UserId");

            //Get participant that is leaving
            Participant leaving_participant = activity_participants.FirstOrDefault(leave => leave.UserId == user_in_session);

            //Remove participant from list of participants
            dbContext.Participants.Remove(leaving_participant);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        //Delete an Activity
        [HttpGet("participate/{ActivityId}/delete")]
        public IActionResult DeleteActivity(int ActivityId)
        {
            //Get Activity matching passed in ActivityId
            Activity activity = dbContext.Activities.Include(act => act.Participants).FirstOrDefault(act => act.ActivityId == ActivityId);

            //Get list of participants from the activity
            var list_of_participants = activity.Participants.ToList();

            //Remove each participant from the list of participants
            foreach(var part in list_of_participants)
            {
                dbContext.Participants.Remove(part);
            }

            //Delete the Activity from the list of activities
            dbContext.Activities.Remove(activity);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        [HttpGet("activity/{ActivityId}")]
        public IActionResult ShowActivity(int ActivityId)
        {
           //Get the activity including Coordinator/Participants/Users that matches the ActivityId passed in 
           Activity activities = dbContext.Activities.Include(act => act.Coordinator)
                                                .Include(act => act.Participants)
                                                    .ThenInclude(u => u.User)
                                                .FirstOrDefault(a => a.ActivityId == ActivityId);

            //Grab user object that matches logged in user's id from session
            User logged_in_user = dbContext.Users.FirstOrDefault(user => user.UserId == HttpContext.Session.GetInt32("UserId"));

            //Store that user in ViewBag
            ViewBag.Logged_in_user = logged_in_user;

            return View(activities);
        }

        //Edit/Update an activity
        [HttpGet("Edit/{ActivityId}")]
        public IActionResult Edit(int ActivityId)
        {
            //Get the activity including Coordinator/Participants/Users that matches the ActivityId passed in 
            Activity activity = dbContext.Activities.Include(act => act.Coordinator)
                                                .Include(act => act.Participants)
                                                    .ThenInclude(u => u.User)
                                                .FirstOrDefault(a => a.ActivityId == ActivityId);

            return View(activity);
        }

        //Update an activity
        [HttpPost("Update/{ActivityId}")]
        public IActionResult Update(Activity edit_activity, int activityId)
        {
            if(ModelState.IsValid){
            //Get the activity including Coordinator/Participants/Users that matches the ActivityId passed in 
            Activity activity = dbContext.Activities.Include(act => act.Coordinator)
                                                .Include(act => act.Participants)
                                                    .ThenInclude(u => u.User)
                                                .FirstOrDefault(a => a.ActivityId == activityId);

            // Activity activity = dbContext.Activities.FirstOrDefault(a => a.ActivityId == activityId);

            activity.Title = edit_activity.Title;
            activity.Date = edit_activity.Date;
            activity.Time = edit_activity.Time;
            activity.Duration = edit_activity.Duration;
            // activity.DurationMeasure = edit_activity.DurationMeasure;
            activity.Description = edit_activity.Description;
            // activity.Coordinator = edit_activity.Coordinator;
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
            }

            Activity activ = dbContext.Activities.FirstOrDefault(a => a.ActivityId == activityId);

            return View("Edit", activ);

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
