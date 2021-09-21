using PresetManager;
using PresetManager.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebcamTimelapseNET5
{
    class TimelapseSettings : AppSettings
    {
        [Control("DiffLowestThreshold")]
        public double diffLowestThreshold = 1;
        [Control("DiffHighestThreshold")]
        public double diffHighestThreshold = 4;
        [Control("MaxFramesPerFrame")]
        public double maxFramesPerFrame = 120;
        [Control("MinFramesPerFrame")]
        public double minFramesPerFrame = 20;
        [Control("FramesPerFrameMaxStepUp")]
        public double framesPerFrameMaxStepUp = 10;
        [Control("FramesPerFrameMaxStepDown")]
        public double framesPerFrameMaxStepDown = 300;
    }
}
