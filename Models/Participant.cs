using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CSharpBelt.Models
{
    public class Participant
    {
        [Key]
        public int ParticipantId{get;set;}
        public int ActivityId{get;set;}
        public int UserId{get;set;}
        public bool Joined{get;set;}

        public Participant(int ActivityId, int UserId)
        {
            this.ActivityId = ActivityId;
            this.UserId = UserId;
            this.Joined = true;
        }
        
        //Navigation Properties

        public Activity Activity{get;set;}
        public User User{get;set;}
    }
}