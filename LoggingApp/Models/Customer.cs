﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LoggingApp.Models
{
    public class Customer
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public int Age { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}