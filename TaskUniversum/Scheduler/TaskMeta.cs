﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaskUniversum.Task
{
    // utilisation between module interface and broker
    public class MetaTask
    {
        public string ChannelName;
        public string NameAndDescription;
        public string ModuleName { get; set; }
        public IntervalType intervalType { get; set; }
        public long intervalValue { get; set; }
    }
}