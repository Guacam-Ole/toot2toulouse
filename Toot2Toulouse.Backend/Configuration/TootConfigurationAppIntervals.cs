using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfigurationAppIntervals
    {
        [OverviewCategory("Intervals", "Look for new Toots", Suffix = " (hh:mm:ss)")]
        public TimeSpan Sending { get; set; }   // How often should t2t check for message and send?

        public TimeSpan FollowerCheckApp { get; set; }  // How often should t2t scan for follower informations?
        public TimeSpan FollwerCheckUser { get; set; }  // How often should t2t scan for follower informations (per user)?

        [OverviewCategory("Intervals", "Minimum Delay", Suffix = " (hh:mm:ss)")]
        public TimeSpan MinDelay { get; set; } // Minimum delay before tweeting a toot

        [OverviewCategory("Intervals", "Maximum Delay", Suffix = " (hh:mm:ss)")]
        public TimeSpan MaxDelay { get; set; } // Maximum delay before tweeting a toot

        public int AuthFailureDeleteDays { get; set; } = 14;
    }
}
