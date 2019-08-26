using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CSharpBelt.Models;

namespace CSharpBelt
{
    public class Activity
    {
        public int ActivityId { get; set; }
        [Required(ErrorMessage="Title is required.")]
        public string Title { get; set; }

        [Required]
        [CheckIfFuture]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [DataType(DataType.Time)]
        public DateTime Time { get; set; }
        public int Duration { get; set; }
        [Required(ErrorMessage="Description is required.")]

        public string DurationMeasure { get; set; }
        public string Description { get; set; }
        public int UserId {get;set;}
        public User Coordinator { get; set; }

        public List<Participant> Participants { get; set; }
        public Activity()
        {
            Participants = new List<Participant>();
        }
    }

    public class CheckIfFuture : ValidationAttribute
    {
        protected override ValidationResult IsValid(object date, ValidationContext validationContext){
        DateTime day = Convert.ToDateTime(date);
        DateTime now  =  DateTime.Now;
        if(day<now){
            return new ValidationResult("Activity cannot occur in the past. Please select a future date.");
        }else{
            return ValidationResult.Success;
        }
    }

    }
}