using AForge.Video.DirectShow;

namespace CameraServer
{
    [Flags]
    public enum ControlFlag
    {
        /// <summary>
        /// No control flag.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Auto control Flag.
        /// </summary>
        Auto = 0x0001,

        /// <summary>
        /// Manual control Flag.
        /// </summary>
        Manual = 0x0002,
    }

    public class CameraSetting
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Default { get; set; }

        public int Value { get; set; }

        public int StepSize { get; set; }

        public ControlFlag ControlValue { get; set; }
        public ControlFlag ControlRange { get; set; }

        public CameraControlProperty? CamProperty { get; set; }
        public VideoProcAmpProperty? VideoProperty { get; set; }
    }
}
