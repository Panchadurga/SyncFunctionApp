﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductsFunctionApp.Models
{
    public class SysBProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Category { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
    }
}
