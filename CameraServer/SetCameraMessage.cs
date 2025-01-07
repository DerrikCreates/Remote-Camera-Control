namespace CameraServer
{
    public class SetCameraMessage
    {
        ///the camera name
        public string Name { get; set; }

        /// this will ignore all the other settings and set the enitre camera to default
        public int? SetToDefault { get; set; }

        public int? Focus { get; set; }
        public int? AutoFocus { get; set; }
	// TODO: make a setting to enable auto exposure 
        public int? Exposure { get; set; }

        public int? Brightness { get; set; }
        public int? Contrast { get; set; }
        public int? Satuation { get; set; }
        public int? Sharpness { get; set; }
	// TODO: make settings to enable auto whitebalance
        public int? WhiteBalance { get; set; }
        public int? BacklightComp { get; set; }

        public int? Gain { get; set; }
    }
}
