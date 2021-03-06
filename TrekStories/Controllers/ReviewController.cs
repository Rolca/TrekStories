﻿using Microsoft.AspNet.Identity;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TrekStories.Abstract;
using TrekStories.DAL;
using TrekStories.Models;
using TrekStories.Utilities;
using System.Data.Entity;


namespace TrekStories.Controllers
{
    [RequireHttps]
    [Authorize]
    public class ReviewController : Controller
    {
        private const string IMAGES_CONTAINER_NAME = "trekstories-reviewimages-blobcontainer";

        private ITrekStoriesContext db = new TrekStoriesContext();
        private BlobUtility utility;

        public ReviewController()
        {
            utility = new BlobUtility();
        }

        public ReviewController(ITrekStoriesContext context)
        {
            db = context;
            utility = new BlobUtility();
        }

        // GET: Review/Create
        public async Task<ActionResult> Create(int? id)  //stepId
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Step step = await db.Steps.Include(s => s.Review).FirstOrDefaultAsync(s => s.StepId == id);
            if (step == null)
            {
                return View("CustomisedError", new HandleErrorInfo(
                                new UnauthorizedAccessException("Oops, the step you are trying to review doesn't seem to exist. Please try navigating to the main page again."),
                                "Trip", "Index"));
            }
            if (step.Review != null)
            {
                return View("CustomisedError", new HandleErrorInfo(
                                new ArgumentException("Oops, this step has already been reviewed, please edit the existing comment instead."),
                                "Trip", "Index"));
            }
            ViewBag.StepId = id.Value;
            ViewBag.From = step.From;
            ViewBag.To = step.To;
            ViewBag.Rating = 0;
            ViewBag.PhotoCount = 0;
            ViewBag.Create = true;
            return View("Edit", new Review() { StepId = id.Value});
        }

        // GET: Review/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Step step = await db.Steps.Include(s => s.Review).FirstOrDefaultAsync(s => s.StepId == id);
            if (step == null)
            {
                return View("CustomisedError", new HandleErrorInfo(
                                new UnauthorizedAccessException("Oops, the step you are trying to review doesn't seem to exist. Please try navigating to the main page again."),
                                "Trip", "Index"));
            }
            ViewBag.Rating = step.Review.Rating;
            ViewBag.StepId = id.Value;
            ViewBag.From = step.From;
            ViewBag.To = step.To;
            ViewBag.PhotoCount = StepController.GetReviewPicturesCount(step);
            ViewBag.Create = false;
            return View(step.Review);
        }

        // POST: Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Review review)
        {
            //check if the step belongs to authenticated user!
            Step step = await db.Steps.FindAsync(review.StepId);
            if (step.Trip.TripOwner != User.Identity.GetUserId())
            {
                return View("CustomisedError", new HandleErrorInfo(
                                new UnauthorizedAccessException("Oops, this review doesn't seem to be yours, you cannot add nor edit it."),
                                "Trip", "Index"));
            }
            if (ModelState.IsValid)
            {
                if (review.ReviewId == 0)
                {
                    review.ReviewId = review.StepId;
                    db.Reviews.Add(review);
                }
                else
                {
                    db.MarkAsModified(review);
                }
                await db.SaveChangesAsync();
                return RedirectToAction("Details", "Step", new { id = review.StepId });
            }
            else
            {
                ViewBag.StepId = review.StepId;
                ViewBag.From = step.From;
                ViewBag.To = step.To;
                ViewBag.Create = true;
                return View(review);
            }    
        }

        [HttpPost]
        public async Task<ActionResult> UploadImageAsync(HttpPostedFileBase file, int revId)
        {
            if (file != null)
            {
                file = file ?? Request.Files["file"];
                string error = await ErrorForNullReviewOrWrongOwner(revId);
                if (error != null)
                {
                     return View("CustomisedError", new HandleErrorInfo(
                                new UnauthorizedAccessException(error),"Trip", "Index"));
                }
                try
                {
                    string result = await utility.UploadBlobAsync(file, IMAGES_CONTAINER_NAME);
                    Image uploadedImage = new Image { ReviewId = revId, Url = result };
                    db.Images.Add(uploadedImage);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    TempData["message"] = e.Message;
                    return RedirectToAction("Edit", new { id = revId });
                }
            }
            else
            {
                TempData["message"] = "Please browse for a file to upload.";
            }
            ViewBag.Create = false;
            return new RedirectResult(Url.Action("Edit", new { id = revId }) + "#AddPhoto");
        }

        private async Task<string> ErrorForNullReviewOrWrongOwner(int revId)
        {
            Review review = await db.Reviews.FindAsync(revId);
            if (review == null)
            {
                return "Oops, the review you want to add images to does not exist.";
            }
            if (review.Step.Trip.TripOwner != User.Identity.GetUserId())
            {
                return "Oops, this review doesn't seem to be yours, you cannot add images to it.";
            }
            else
            {
                return null;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteImageAsync(int ImgId)
        {
            Image imageToDelete = await db.Images.FindAsync(ImgId);
            if (imageToDelete == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (imageToDelete.Review.Step.Trip.TripOwner != User.Identity.GetUserId())
            {
                return View("CustomisedError", new HandleErrorInfo(
                                new UnauthorizedAccessException("Oops, this review doesn't seem to be yours, you cannot delete its images."),
                                "Trip", "Index"));
            }
            //remove from database
            db.Images.Remove(imageToDelete);
            db.SaveChanges();
            //remove from Cloud Storage
            try
            {
                await utility.DeleteBlobAsync(imageToDelete.Url, IMAGES_CONTAINER_NAME);
            }
            catch (Exception)
            {
                TempData["message"] = "There was an error when deleting the file from Blob Storage.";
            }
            return new RedirectResult(Url.Action("Edit", new { id = imageToDelete.ReviewId }) + "#AddPhoto");
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
