﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrekStories.Models
{
    public class Review
    {
        [Key, ForeignKey("Step")]
        public int ReviewId { get; set; }
        [Range(1,5, ErrorMessage = "Please assign a rating between 1 and 5.")]
        public int Rating { get; set; }

        [Display(Name = "Private Notes")]
        [DataType(DataType.MultilineText)]
        [StringLength(2000, ErrorMessage = "Notes are limited to 2000 characters maximum.")]
        public string PrivateNotes { get; set; }

        [Display(Name = "Public Review")]
        [DataType(DataType.MultilineText)]
        [StringLength(2000, ErrorMessage = "Notes are limited to 2000 characters maximum.")]
        public string PublicNotes { get; set; }

        public virtual ICollection<Image> Images { get; set; }

        [Required]
        public int StepId { get; set; }
        public virtual Step Step { get; set; }
    }
}