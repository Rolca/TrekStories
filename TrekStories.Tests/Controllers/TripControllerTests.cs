﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using TrekStories.Models;
using TrekStories.Tests;

namespace TrekStories.Controllers.Tests
{
    [TestClass()]
    public class TripControllerTests
    {
        [TestMethod()]
        public void TripControllerTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void TripControllerTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void IndexTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DetailsTest()
        {
            
            // Trip t = (Trip)((ViewResult)result).Model;
            // Assert.AreEqual(t.Title, "Test Trip");
            //Assert.AreEqual(t.TripCategory, TripCategory.forest);
            //Assert.AreEqual(t.Duration, 0);
            //Assert.AreEqual(t.TotalCost, 0);
            //Assert.AreEqual(t.TotalWalkingDistance, 0);

            Assert.Fail();
        }

        [TestMethod()]
        public void CanValidateTrip()
        {
            Trip newTrip = new Trip
            {
                Title = "Test Trip",
                Country = "Ireland",
                TripCategory = TripCategory.forest,
                StartDate = new DateTime(2015, 4, 12),
                TripOwner = "ABC123"
            };
            var context = new ValidationContext(newTrip, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(newTrip, context, result, true);

            Assert.IsTrue(valid);
        }

        [TestMethod()]
        public void DoesNotValidateTripWithNoName()
        {
            // Arrange
            Trip newTrip = new Trip
            {
                Title = "",
                Country = "Ireland",
                TripCategory = TripCategory.forest,
                StartDate = new DateTime(2015, 4, 12),
                TripOwner = "ABC123"
            };
            var context = new ValidationContext(newTrip, null, null);
            var result = new List<ValidationResult>();

            // Act
            var valid = Validator.TryValidateObject(newTrip, context, result, true);

            Assert.IsFalse(valid);
            Assert.AreEqual(result.First().ErrorMessage, "Please give your trip a descriptive title.");
        }

        [TestMethod()]
        public void CanCreateTrip()
        {
            TestTrekStoriesContext tc = new TestTrekStoriesContext();
            Trip newTrip = new Trip
            {
                Title = "Test Trip",
                Country = "Ireland",
                TripCategory = TripCategory.forest,
                StartDate = new DateTime(2015, 4, 12),
                TripOwner = "ABC123"
            };

            var controller = new TripController(tc);
            var result = controller.Create(newTrip) as RedirectToRouteResult;

            Assert.AreEqual("Index", result.RouteValues["action"]);            
        }

        [TestMethod()]
        public void CannotCreateTripWithModelErrors()
        {
            TestTrekStoriesContext tc = new TestTrekStoriesContext();
            Trip newTrip = new Trip
            {
                Title = "",
                Country = "Ireland",
                TripCategory = TripCategory.forest,
                StartDate = new DateTime(2015, 4, 12),
                TripOwner = "ABC123"
            };

            var controller = new TripController(tc);
            controller.ModelState.AddModelError("", "Error");
            var result = controller.Create(newTrip) as ViewResult;

            Assert.AreEqual("", result.ViewName);
            Assert.AreEqual(false, result.ViewData.ModelState.IsValid);
            Assert.IsNotNull(result.ViewData.ModelState[""].Errors);
        }

        [TestMethod()]
        public void EditTripReturnsCorrectDetails()
        {
            TestTrekStoriesContext tc = new TestTrekStoriesContext();

            var expectedTrip = new Trip
            {
                TripId = 1,
                Title = "Test Trip",
                Country = "Ireland",
                TripCategory = TripCategory.forest,
                StartDate = new DateTime(2015, 4, 12),
                TripOwner = "ABC123",
                TotalWalkingDistance = 45
            };

            tc.Trips.Add(expectedTrip);
            var controller = new TripController(tc);

            // act
            var result = controller.Edit(1) as ViewResult;
            var resultData = (Trip)result.ViewData.Model;

            // assert
            Assert.AreEqual(expectedTrip.Title, resultData.Title);
            Assert.AreEqual(expectedTrip.Country, resultData.Country);
            Assert.AreEqual(expectedTrip.TotalWalkingDistance, resultData.TotalWalkingDistance);
        }

        [TestMethod]
        public void Cannot_Edit_Nonexistent_Product()
        {
            TestTrekStoriesContext tc = new TestTrekStoriesContext();
            var expectedTrip = new Trip
            {
                TripId = 1,
                Title = "Test Trip",
                Country = "Ireland",
                TripCategory = TripCategory.forest,
                StartDate = new DateTime(2015, 4, 12),
                TripOwner = "ABC123",
            };
            tc.Trips.Add(expectedTrip);
            var controller = new TripController(tc);

            // Act
            var badResult = controller.Edit(2);
            // Assert
            Assert.IsInstanceOfType(badResult, typeof(HttpNotFoundResult));
        }
    }
}