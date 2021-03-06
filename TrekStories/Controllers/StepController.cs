﻿using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using TrekStories.Abstract;
using TrekStories.DAL;
using TrekStories.Models;

namespace TrekStories.Controllers
{
    [RequireHttps]
    [Authorize]
    public class StepController : Controller
    {
        private const string NULL_STEP_ERROR = "Oops, the step you are looking for doesn't seem to exist. Please try navigating to the main page again.";

        private ITrekStoriesContext db = new TrekStoriesContext();

        public StepController() { }

        public StepController(ITrekStoriesContext context)
        {
            db = context;
        }

        // GET: Step/Details/5
        [AllowAnonymous]
        public async Task<ActionResult> Details(int id = 1)
        {
            Step step = await db.Steps.Include(s => s.Accommodation).Include(s => s.Review).FirstOrDefaultAsync(s => s.StepId == id);
            if (step == null)
            {
                return View("CustomisedError", new HandleErrorInfo(
                                new UnauthorizedAccessException(NULL_STEP_ERROR), "Trip", "Index"));
            }
            //create array for pagination in view
            ViewBag.Steps = await db.Steps.Where(s => s.TripId == step.TripId).OrderBy(s =>s.SequenceNo).Select(s => s.StepId).ToArrayAsync();

            //create activity thread
            ViewBag.ActivityThread = await CreateActivityThread(step);
            ViewBag.HideReview = step.Date > DateTime.Today ? "hidden" : "";
            ViewBag.HideActions = step.Trip.TripOwner != User.Identity.GetUserId() ? "hidden" : "";
            ViewBag.PhotoCount = GetReviewPicturesCount(step);

            return View(step);
        }

        [NonAction]
        public static int GetReviewPicturesCount(Step step)
        {
            if (step.Review == null)
            {
                return 0;
            }
            else
            {
                return step.Review.Images.Count;
            }
        }

        // GET: Step/Create
        public async Task<ActionResult> Create(int? tripId, int? seqNo)
        {
            if (tripId == null || seqNo == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            try
            {
                await CheckTripNotNullOrNonOwner(tripId);
            }
            catch (UnauthorizedAccessException ex)
            {
                return View("CustomisedError", new HandleErrorInfo(ex, "Trip", "Index"));
            }
            ViewBag.SeqNo = seqNo;
            return View("Create", new StepViewModel());
        }

        private async Task CheckTripNotNullOrNonOwner(int? tripId)
        {
            Trip trip = await db.Trips.FindAsync(tripId);
            if (trip == null)
            {
                throw new UnauthorizedAccessException(NULL_STEP_ERROR);
            }
            if (trip.TripOwner != User.Identity.GetUserId())
            {
                throw new UnauthorizedAccessException("Oops, this trip doesn't seem to be yours, you cannot add a step to it.");
            }
            ViewBag.TripId = tripId;
            ViewBag.TripTitle = trip.Title;
        }

        // POST: Step/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(StepViewModel stepViewModel)
        {
            try
            {
                await CheckTripNotNullOrNonOwner(stepViewModel.TripId);
            }
            catch (UnauthorizedAccessException ex)
            {
                return View("CustomisedError", new HandleErrorInfo(ex, "Trip", "Index"));
            }
            try
            {
                if (ModelState.IsValid)
                {
                    Step newStep = new Step()
                    {
                        SequenceNo = stepViewModel.SequenceNo,
                        From = stepViewModel.From,
                        To = stepViewModel.To,
                        WalkingTime = stepViewModel.WalkingTimeHours + stepViewModel.WalkingTimeMinutes/60.0,
                        WalkingDistance = stepViewModel.WalkingDistance,
                        Ascent = stepViewModel.Ascent,
                        Description = stepViewModel.Description,
                        Notes = stepViewModel.Notes,
                        TripId = stepViewModel.TripId
                    };
                    //retrieve all subsequent steps and update seq no
                    foreach (Step item in db.Steps.Where(s => s.TripId == newStep.TripId && s.SequenceNo >= newStep.SequenceNo))
                    {
                        item.SequenceNo++;
                    }

                    db.Steps.Add(newStep);
                    await db.SaveChangesAsync();

                    //retrieve all steps where seq no >= to new step.seq no in an array including new step and assign accommodation of previous step for that seq no 
                    Step[] subsequentSteps = await db.Steps.Where(s => s.TripId == newStep.TripId && s.SequenceNo >= newStep.SequenceNo).OrderBy(s => s.SequenceNo).ToArrayAsync();
                    for (int i = 0; i <subsequentSteps.Length-1; i++)
                    {
                        subsequentSteps[i].AccommodationId = subsequentSteps[i + 1].AccommodationId;
                    }
                    //set last one to null
                    subsequentSteps[subsequentSteps.Length - 1].AccommodationId = null;

                    await db.SaveChangesAsync();
                    return RedirectToAction("Details", new { id = newStep.StepId });
                }
            }
            catch (RetryLimitExceededException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, contact the system administrator.");
            }
            ViewBag.SeqNo = stepViewModel.SequenceNo;
            return View(stepViewModel);
        }

        // GET: Step/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Step step = await db.Steps.FindAsync(id);
            string nullOrOwnerError = StepNullOrNotOwnedByUserError(step);
            if (nullOrOwnerError != "")
            {
                return View("CustomisedError", new HandleErrorInfo(new UnauthorizedAccessException(nullOrOwnerError), "Trip", "Index"));
            }
            StepViewModel stepToEdit = new StepViewModel()
            {
                StepId = step.StepId,
                SequenceNo = step.SequenceNo,
                From = step.From,
                To = step.To,
                WalkingTimeHours = (int)step.WalkingTime,
                WalkingTimeMinutes = (int)((step.WalkingTime%1) * 60),
                WalkingDistance = step.WalkingDistance,
                Ascent = step.Ascent,
                Description = step.Description,
                Notes = step.Notes,
                TripId = step.TripId
            };
            return View(stepToEdit);
        }

        // POST: Step/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(StepViewModel vm)
        {
            if (vm == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Step stepToUpdate = await db.Steps.Include(t => t.Trip).FirstOrDefaultAsync(x => x.StepId == vm.StepId.Value);
            string nullOrOwnerError = StepNullOrNotOwnedByUserError(stepToUpdate);
            if (nullOrOwnerError != "")
            {
                return View("CustomisedError", new HandleErrorInfo(new UnauthorizedAccessException(nullOrOwnerError), "Trip", "Index"));
            }
            if (ModelState.IsValid)
            {
                try
                {
                    //assign vm values to step
                    stepToUpdate.Ascent = vm.Ascent;
                    stepToUpdate.Description = vm.Description;
                    stepToUpdate.From = vm.From;
                    stepToUpdate.Notes = vm.Notes;
                    stepToUpdate.To = vm.To;
                    stepToUpdate.WalkingDistance = vm.WalkingDistance;
                    stepToUpdate.WalkingTime = vm.WalkingTimeHours + vm.WalkingTimeMinutes / 60.0;

                    await db.SaveChangesAsync();
                    return RedirectToAction("Details", "Trip", new { id = stepToUpdate.TripId});
                }
                catch (RetryLimitExceededException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, contact the system administrator.");
                }
            }
            return View(vm);
        }

        // GET: Step/Delete/5
        public async Task<ActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (saveChangesError.GetValueOrDefault())
            {
                ViewBag.ErrorMessage = "Delete failed. Please try again, and if the problem persists, contact the system administrator.";
            }
            Step step = await db.Steps.FindAsync(id);

            string nullOrOwnerError = StepNullOrNotOwnedByUserError(step);
            if (nullOrOwnerError != "")
            {
                return View("CustomisedError", new HandleErrorInfo(new UnauthorizedAccessException(nullOrOwnerError), "Trip", "Index"));
            }

            string AccommodationOrIamgesNotNullError = AccommodationOrReviewImagesNotNullError(step);
            if (AccommodationOrIamgesNotNullError != "")
            {
                TempData["message"] = AccommodationOrIamgesNotNullError;
                return RedirectToAction("Details", "Step", new { id = step.StepId });
            }

            return View(step);
        }

        // POST: Step/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Step stepToDelete = await db.Steps.Include(s => s.Trip).Include(s => s.Accommodation).Include(s => s.Activities).Include(s => s.Review).SingleOrDefaultAsync(s => s.StepId == id);
            try
            {
                string nullOrOwnerError = StepNullOrNotOwnedByUserError(stepToDelete);
                if (nullOrOwnerError != "")
                {
                    return View("CustomisedError", new HandleErrorInfo(new UnauthorizedAccessException(nullOrOwnerError), "Trip", "Index"));
                }
                string AccommodationOrIamgesNotNullError = AccommodationOrReviewImagesNotNullError(stepToDelete);
                if (AccommodationOrIamgesNotNullError != "")
                {
                    TempData["message"] = AccommodationOrIamgesNotNullError;
                    return RedirectToAction("Details", "Step", new { id = stepToDelete.StepId });
                }
                
                //retrieve all subsequent steps and update seq no
                foreach (Step step in db.Steps.Where(s => s.TripId == stepToDelete.TripId))
                {
                    if (step.SequenceNo > stepToDelete.SequenceNo)
                    {
                        if (step.Accommodation != null)
                        {
                            TempData["message"] = "One of the following steps has an accommodation which first need to be deleted or moved to a previous step.";
                            return RedirectToAction("Details", "Step", new { id = stepToDelete.StepId });
                        }
                        else
                        {
                            step.SequenceNo--;
                        }   
                    }
                }

                foreach (var item in stepToDelete.Activities)
                {
                    stepToDelete.Trip.TotalCost -= item.Price;
                }

                db.Reviews.Remove(stepToDelete.Review);
                db.Steps.Remove(stepToDelete);
                await db.SaveChangesAsync();
            }
            catch (RetryLimitExceededException)
            {
                return RedirectToAction("Delete", new { id = id, saveChangesError = true});
            }
            return RedirectToAction("Details", "Trip", new { id = stepToDelete.TripId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private string StepNullOrNotOwnedByUserError(Step step)
        {
            if (step == null)
            {
                return "Oops, it looks like you are trying to access a step that does not exist. Please try navigating to the main page again.";
            }
            else if (step.Trip.TripOwner != User.Identity.GetUserId())
            {
                return "Oops, this step doesn't seem to be yours, you cannot perform this action.";
            }
            else
            {
                return "";
            }
        }

        private string AccommodationOrReviewImagesNotNullError(Step step)
        {
            if (step.Accommodation != null)
            {
                return string.Format("Step " + step.SequenceNo + " cannot be deleted because it is linked to an accommodation. " +
                    "Please first edit or delete the accommodation for the step.");
            }
            Review rev = step.Review;
            if (rev != null)
            {
                if (rev.Images.Count > 0)
                {
                   return string.Format("Step " + step.SequenceNo + " cannot be deleted because it is linked to a review with images. " +
                    "Please first delete the images.");
                }
            }
            return "";
        }


        [NonAction]
        public async Task<List<ActivityThreadViewModel>> CreateActivityThread(Step step)
        {
            List<ActivityThreadViewModel> activityThread = new List<ActivityThreadViewModel>();

            await AddLeisureToActivityThread(activityThread, step.StepId);
            await AddTransportToActivityThread(activityThread, step);
            AddAccommodationToActivityThread(activityThread, step);

            return activityThread.OrderBy(a => a.StartTime.TimeOfDay).ToList();
        }

        private async Task AddLeisureToActivityThread(List<ActivityThreadViewModel> activityThread, int stepId)
        {
            foreach (LeisureActivity activity in await db.Activities.OfType<LeisureActivity>().Where(a => a.StepId == stepId).ToListAsync())
            {
                activityThread.Add(new ActivityThreadViewModel
                {
                    ID = activity.ID,
                    StartTime = activity.StartTime,
                    Name = activity.Name,
                    Price = activity.Price,
                    Icon = GetLeisureIcon(activity.LeisureCategory.ToString()),
                    Controller = "Activities"
                });
            }
        }

        private async Task AddTransportToActivityThread(List<ActivityThreadViewModel> activityThread, Step step)
        {
            var transportActivities = db.Activities.OfType<Transport>();

            var stepActivities = await transportActivities.Where(a => a.StepId == step.StepId).ToListAsync();
            AddStepTransport(ref activityThread, stepActivities, false);
            
            var transportsArrivingOnDay = (from s in step.Trip.Steps
                                           join a in transportActivities
                                           on s.StepId equals a.StepId
                                           where a.GetArrivalTime().Date == step.Date.Date
                                           select a).Except(stepActivities).ToList();
            AddStepTransport(ref activityThread, transportsArrivingOnDay, true);
        }

        private void AddStepTransport(ref List<ActivityThreadViewModel> activityThread, List<Transport> stepTransport, bool arrival)
        {
            foreach (Transport activity in stepTransport)
            {
                ActivityThreadViewModel activityVm = new ActivityThreadViewModel
                {
                    ID = activity.ID,
                    Price = activity.Price,
                    Icon = GetTransportIcon(activity.TransportType.ToString()),
                    Controller = "Activities"
                };

                if (arrival)
                {
                    activityVm.StartTime = activity.GetArrivalTime();
                    activityVm.Name = "Arrival " + activity.Name;
                    activityVm.ArrivalTime = null;
                }
                else
                {
                    activityVm.StartTime = activity.StartTime;
                    activityVm.Name = activity.Name;
                    activityVm.ArrivalTime = activity.GetArrivalTime();
                }
                activityThread.Add(activityVm);
            }
        }

        private void AddAccommodationToActivityThread(List<ActivityThreadViewModel> activityThread, Step step)
        {
            //Add check-in if happening on step date
            if (step.Accommodation != null)
            {
                AddChekInToActivityThread(activityThread, step.Accommodation, step.Date);
            }

            //Add check-out if happening on step date
            AddChekOutToActivityThread(activityThread, step);
        }

        private void AddChekInToActivityThread(List<ActivityThreadViewModel> activityThread, Accommodation accommodation, DateTime date)
        {
            //needs to search in accommodations for matching check-in
            if (accommodation.CheckIn.Date == date.Date)
            {
                activityThread.Add(new ActivityThreadViewModel
                {
                    ID = accommodation.AccommodationId,
                    StartTime = accommodation.CheckIn,
                    Name = "Check-In at " + accommodation.Name,
                    Price = accommodation.Price,
                    Icon = "fas fa-bed",
                    Controller = "Accommodation"
                });
            }
        }

        private void AddChekOutToActivityThread(List<ActivityThreadViewModel> activityThread, Step step)
        {
            var tripAccommodation = (from s in step.Trip.Steps
                                     join a in db.Accommodations
                                     on s.AccommodationId equals a.AccommodationId
                                     where a.CheckOut.Date == step.Date.Date
                                     select a).Distinct().SingleOrDefault();
            if (tripAccommodation != null)
            {
                activityThread.Add(new ActivityThreadViewModel
                {
                    ID = tripAccommodation.AccommodationId,
                    StartTime = tripAccommodation.CheckOut,
                    Name = "Check-Out at " + tripAccommodation.Name,
                    Price = tripAccommodation.Price,
                    Icon = "fas fa-bed",
                    Controller = "Accommodation"
                });
            }
        }

        public string GetTransportIcon(string type)
        {
            switch (type)
            {
                case "boat": return "fas fa-ship";
                case "plane": return "fas fa-plane";
                case "train":
                case "tram":
                case "metro":
                    { return "fas fa-subway"; }
                case "bus": return "fas fa-bus";
                case "car": return "fas fa-car";
                case "hitchhiking": return "fas fa-thumbs-up";
                case "bike": return "fas fa-bicycle";
                case "foot": return "fas fa-walking";

                default: return "";
            }
        }

        public string GetLeisureIcon(string type)
        {
            switch (type)
            {
                case "aquatic": return "fas fa-swimmer";
                case "sports": return "fas fa-football-ball";
                case "musical": return "fas fa-music";
                case "cultural": return "fas fa-university";
                case "nature": return "fas fa-paw";
                case "gastronomy": return "fas fa-utensils";
                case "other": return "fas fa-puzzle-piece";

                default: return "";
            }
        }
    }
}
