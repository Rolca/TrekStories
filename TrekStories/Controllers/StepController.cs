﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TrekStories.Abstract;
using TrekStories.DAL;
using TrekStories.Models;

namespace TrekStories.Controllers
{
    public class StepController : Controller
    {
        private ITrekStoriesContext db = new TrekStoriesContext();

        public StepController() { }

        public StepController(ITrekStoriesContext context)
        {
            db = context;
        }

        // GET: Step
        public async Task<ActionResult> Index()
        {
            var steps = db.Steps.Include(s => s.Accommodation).Include(s => s.Review).Include(s => s.Trip);
            return View(await steps.ToListAsync());
        }

        // GET: Step/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Step step = db.Steps.Find(id);
            if (step == null)
            {
                return HttpNotFound();
            }
            return View(step);
        }

        // GET: Step/Create
        public async Task<ActionResult> Create(int? tripId, int? seqNo)
        {
            if (tripId == null || seqNo == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Trip trip = await db.Trips.FindAsync(tripId);
            if (trip == null)
            {
                return HttpNotFound();
            }

            //ViewBag.AccommodationId = new SelectList(db.Accommodations, "AccommodationId", "Name");
            //ViewBag.StepId = new SelectList(db.Reviews, "ReviewId", "PrivateNotes");
            ViewBag.TripId = tripId;
            ViewBag.SeqNo = seqNo;
            ViewBag.TripTitle = trip.Title;
            return View("Create");
        }

        // POST: Step/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(StepViewModel stepViewModel)
        {
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
                    
                    db.Steps.Add(newStep);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Details", new { id = newStep.StepId });
                }
            }
            catch (DataException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, contact the system administrator.");
            }
            //ViewBag.AccommodationId = new SelectList(db.Accommodations, "AccommodationId", "Name", step.AccommodationId);
            //ViewBag.StepId = new SelectList(db.Reviews, "ReviewId", "PrivateNotes", step.StepId);
            //ViewBag.TripId = new SelectList(db.Trips, "TripId", "Title", step.TripId);
            ViewBag.TripId = stepViewModel.TripId;
            ViewBag.SeqNo = stepViewModel.SequenceNo;
            ViewBag.TripTitle = await (from t in db.Trips
                                       where t.TripId == stepViewModel.TripId
                                       select t.Title).ToListAsync();
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
            if (step == null)
            {
                return HttpNotFound();
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

            Step stepToUpdate = await db.Steps.FindAsync(vm.StepId.Value);
            if (stepToUpdate == null)
            {
                return HttpNotFound();
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
                catch (DataException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, contact the system administrator.");
                }
            }
            return View(vm);
        }

        // GET: Step/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Step step = db.Steps.Find(id);
            if (step == null)
            {
                return HttpNotFound();
            }
            return View(step);
        }

        // POST: Step/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Step step = db.Steps.Find(id);
            db.Steps.Remove(step);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
