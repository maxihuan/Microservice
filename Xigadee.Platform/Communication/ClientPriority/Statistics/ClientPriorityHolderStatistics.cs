﻿#region using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion
namespace Xigadee
{
    public class ClientPriorityHolderStatistics: StatusBase
    {
        public override string Name
        {
            get
            {
                return base.Name;
            }

            set
            {
                base.Name = value;
            }
        }

        public int Ordinal { get; set; }
        public int SkipCount { get; set; }
        public long? PriorityCurrent { get; set; }
        public bool IsReserved { get; set; }
        public int? LastReserved { get; set; }
        public double CapacityPercentage { get; set; }
        public string Status { get; set; }
        public string MappingChannel { get; set; }
        public TimeSpan? PollLast { get; set; }
        public MessagingServiceStatistics Client { get; set; }

        public Exception LastException { get; set; }
        public DateTime? LastExceptionTime { get; set; }

        public decimal? PollSuccessRate { get; set; }
    }
}