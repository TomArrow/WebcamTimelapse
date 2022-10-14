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
        public enum InputType
        {
            [Control("radioInputSRGB")]
            SRGB,
            [Control("radioInputRec709Limited")]
            Rec709_LIMITED,
            [Control("radioInputRec709Full")]
            Rec709_FULL
        }
        public InputType inputType = InputType.SRGB;

        public enum OutputType
        {
            [Control("radioOutputSRGB")]
            SRGB,
            [Control("radioOutputRec709Limited")]
            Rec709_LIMITED,
            [Control("radioOutputRec709Full")]
            Rec709_FULL
        }
        public OutputType outputType = OutputType.SRGB;

        public enum DiffType
        {
            [Control("radioAbsDiff")]
            ABSOLUTE,
            [Control("radioRelDiff")]
            RELATIVE
        }

        public DiffType diffType = DiffType.RELATIVE;

        [Control("MinDiffNoiseThreshold")]
        public double minDiffNoiseThreshold = 1f/255f/12.92f; // One unit in the dark of an image in linearized space.
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
        [Control("MaxSimultaneouslyProcessedFrames")]
        public int maxSimultaneouslyProcessedFrames = 100;
    }
}
