﻿using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EventVault.Models
{
    public class Event
    {
        [Key]
        public int? Id { get; set; }
        public string? EventId { get; set; }
        public string? Category { get; set; }
        public string Title { get; set; }
        public string? APIEventUrlPage { get; set; }
        public string? EventUrlPage { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }

        [ForeignKey("Venue")]
        public int? FK_Venue { get; set; }
        public Venue? Venue { get; set; }

        //if event runs several dates
        public DateTime? Date { get; set; }

        //releasedate for ticketavaliability
        public DateTime? TicketsRelease { get; set; }

        //for pricerange
        [Precision(18, 2)]
        public Decimal? HighestPrice { get; set; }

        [Precision(18, 2)]
        public Decimal? LowestPrice { get; set; }
    }
}
