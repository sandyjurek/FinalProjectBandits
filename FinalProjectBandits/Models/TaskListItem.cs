﻿using FinalProjectBandits.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FinalProjectBandits.Models
{
    public class TaskListItem
    {
        //public int CustomerID { get; set; }
        public int Id { get; set; }
        public string TaskTitle { get; set; }
        public string TaskDescription { get; set; }

        [EnumDataType(typeof(ItemStatus))]
        public ItemStatus Status { get; set; }

        [EnumDataType(typeof(ItemCategory))]
        public ItemCategory Category { get; set; }
        public DateTime TaskStartDate { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime DatePosted { get; set; }

        //not sure if below is right, just mirroring the Customer Model
        //public string Id { get; set; }
        //public TaskListItem Item { get; set; }

        //public string CheckedOutBy { get; set; }
        public int CustomerID { get; set; }
        public Customer Customer { get; set; }
        public int? HelperCustomerID { get; set; }
    }
}
