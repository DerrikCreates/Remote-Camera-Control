using System.Text.Json.Serialization;
using AForge.Video.DirectShow;

namespace CameraServer
{
    public class Camera
    {
        public string Name { get; set; } = "";
        public List<CameraSetting> Settings { get; set; } = [];

        [JsonIgnore]
        public VideoCaptureDevice CaptureDevice { get; protected set; }

        public static void ApplyMesage(Camera cam, SetCameraMessage msg)
        {
            if (msg.Focus is not null)
            {
                SetFocus(cam, msg.Focus.Value);
            }

            if (msg.AutoFocus is not null)
            {
                if (Convert.ToBoolean(msg.AutoFocus.Value))
                {
                    SetFocus(cam, 0, true);
                }
            }

            if (msg.Exposure is not null)
            {
                SetExposure(cam, msg.Exposure.Value);
            }
            if (msg.Brightness is not null)
            {
                SetBrightness(cam, msg.Brightness.Value);
            }
            if (msg.Contrast is not null)
            {
                SetContrast(cam, msg.Contrast.Value);
            }
            if (msg.Satuation is not null)
            {
                SetSaturation(cam, msg.Satuation.Value);
            }
            if (msg.Sharpness is not null)
            {
                SetSharpness(cam, msg.Sharpness.Value);
            }
            if (msg.WhiteBalance is not null)
            {
                SetWhiteBalance(cam, msg.WhiteBalance.Value);
            }
            if (msg.BacklightComp is not null)
            {
                SetBacklightComp(cam, msg.BacklightComp.Value);
            }
            if (msg.Gain is not null)
            {
                SetGain(cam, msg.Gain.Value);
            }
        }

        public static bool SetAllToDefault(Camera cam)
        {
            foreach (var prop in Enum.GetValues<CameraControlProperty>())
            {
                var state = SetCamProperty(cam, prop, 0, true);
            }

            foreach (var prop in Enum.GetValues<VideoProcAmpProperty>())
            {
                var state = SetVideoProperty(cam, prop, 0, true);
            }

            return true;
        }

        public static bool SetExposure(Camera cam, int value)
        {
            return SetCamProperty(cam, CameraControlProperty.Exposure, value);
        }

        public static bool SetFocus(Camera cam, int value, bool autoFocus = false)
        {
            if (autoFocus)
            {
                return SetCamProperty(
                    cam,
                    CameraControlProperty.Focus,
                    value,
                    flags: CameraControlFlags.Auto
                );
            }

            return SetCamProperty(cam, CameraControlProperty.Focus, value);
        }

        // Video Proc Amp setting
        public static bool SetBrightness(Camera cam, int value)
        {
            return SetVideoProperty(cam, VideoProcAmpProperty.Brightness, value);
        }

        public static bool SetContrast(Camera cam, int value)
        {
            return SetVideoProperty(cam, VideoProcAmpProperty.Contrast, value);
        }

        public static bool SetSaturation(Camera cam, int value)
        {
            return SetVideoProperty(cam, VideoProcAmpProperty.Saturation, value);
        }

        public static bool SetSharpness(Camera cam, int value)
        {
            return SetVideoProperty(cam, VideoProcAmpProperty.Sharpness, value);
        }

        public static bool SetGamma(Camera cam, int value)
        {
            return SetVideoProperty(cam, VideoProcAmpProperty.Gamma, value);
        }

        public static bool SetWhiteBalance(Camera cam, int value)
        {
            return SetVideoProperty(cam, VideoProcAmpProperty.WhiteBalance, value);
        }

        public static bool SetBacklightComp(Camera cam, int value)
        {
            return SetVideoProperty(cam, VideoProcAmpProperty.BacklightCompensation, value);
        }

        public static bool SetGain(Camera cam, int value)
        {
            return SetVideoProperty(cam, VideoProcAmpProperty.Gain, value);
        }

        /// setDefault == true will ignore value and reset the property
        private static bool SetCamProperty(
            Camera cam,
            CameraControlProperty prop,
            int value,
            bool setDefault = false,
            CameraControlFlags flags = CameraControlFlags.Manual
        )
        {
            if (FindValidCamSetting(cam, prop, out var setting))
            {
                if (setDefault)
                {
                    return SetCameraPropertyInternal(
                        cam.CaptureDevice,
                        prop,
                        setting.Default,
                        (CameraControlFlags)setting.ControlValue
                    );
                }

                value = int.Clamp(value, setting.Min, setting.Max);

                var dir = setting.Value - value;

                if (int.Abs(dir) == setting.StepSize)
                {
                    // no need to "animate" the setting to the targett
                    return SetCameraPropertyInternal(cam.CaptureDevice, prop, value, flags);
                }

                if (dir > 0)
                {
                    // move negativly

                    for (int i = setting.Value; i > value; i -= setting.StepSize)
                    {
                        var r = SetCameraPropertyInternal(cam.CaptureDevice, prop, i, flags);

                        if (r == false)
                        {
                            return false;
                        }
                    }
                }
                else if (dir < 0)
                { // -11      //-2                // 1
                    for (int i = setting.Value; i < value; i += setting.StepSize)
                    {
                        var r = SetCameraPropertyInternal(cam.CaptureDevice, prop, i, flags);
                        if (r == false)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return SetCameraPropertyInternal(cam.CaptureDevice, prop, value, flags);
                }
            }

            return false;
        }

        private static bool SetVideoProperty(
            Camera cam,
            VideoProcAmpProperty prop,
            int value,
            bool setDefault = false,
            VideoProcAmpFlags flags = VideoProcAmpFlags.Manual
        )
        {
            if (FindValidVideoSetting(cam, prop, out var setting))
            {
                if (setDefault)
                {
                    return SetVideoPropertyInternal(
                        cam.CaptureDevice,
                        prop,
                        setting.Default,
                        VideoProcAmpFlags.Auto
                    );
                }

                value = int.Clamp(value, setting.Min, setting.Max);

                var dir = setting.Value - value;

                if (int.Abs(dir) == setting.StepSize)
                {
                    // no need to "animate" the setting to the targett
                    return SetVideoPropertyInternal(cam.CaptureDevice, prop, value, flags);
                }

                if (dir > 0)
                {
                    // move negativly

                    for (int i = setting.Value; i > value; i -= setting.StepSize)
                    {
                        var r = SetVideoPropertyInternal(cam.CaptureDevice, prop, i, flags);

                        if (r == false)
                        {
                            return false;
                        }
                    }
                }
                else if (dir < 0)
                { // -11      //-2                // 1
                    for (int i = setting.Value; i < value; i += setting.StepSize)
                    {
                        var r = SetVideoPropertyInternal(cam.CaptureDevice, prop, i, flags);
                        if (r == false)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return SetVideoPropertyInternal(cam.CaptureDevice, prop, value, flags);
                }
            }

            return false;
        }

        private static bool FindValidCamSetting(
            Camera cam,
            CameraControlProperty prop,
            out CameraSetting setting
        )
        {
            setting = cam.Settings.FirstOrDefault(c =>
                c.CamProperty == prop && c.ControlRange != ControlFlag.None
            );

            if (setting is null)
            {
                return false;
            }
            return true;
        }

        private static bool FindValidVideoSetting(
            Camera cam,
            VideoProcAmpProperty prop,
            out CameraSetting setting
        )
        {
            setting = cam.Settings.FirstOrDefault(c =>
                c.VideoProperty == prop && c.ControlRange != ControlFlag.None
            );

            if (setting is null)
            {
                return false;
            }
            return true;
        }

        public static List<Camera> GetCameras()
        {
            var cameras = new List<Camera>();
            var devices = GetVideoDevices();

            foreach (var deviceInfo in devices)
            {
                var device = deviceInfo.device;
                var name = deviceInfo.name;
                Camera cam = new() { Name = name, CaptureDevice = device };

                cameras.Add(cam);
                foreach (var prop in Enum.GetValues<CameraControlProperty>())
                {
                    try
                    {
                        CameraSetting setting = new();

                        device.GetCameraProperty(prop, out var value, out var flags);
                        setting.CamProperty = prop;
                        setting.Value = value;
                        setting.ControlValue = (ControlFlag)flags;

                        device.GetCameraPropertyRange(
                            prop,
                            out var min,
                            out var max,
                            out var step,
                            out var defaultValue,
                            out var flag
                        );
                        setting.Min = min;
                        setting.Max = max;
                        setting.StepSize = step;
                        setting.Default = defaultValue;
                        setting.ControlRange = (ControlFlag)flag;

                        cam.Settings.Add(setting);
                    }
                    catch (Exception ex)
                    {
                        // cam does not support this property
                    }
                }

                foreach (var prop in Enum.GetValues<VideoProcAmpProperty>())
                {
                    try
                    {
                        CameraSetting setting = new();
                        device.GetVideoProperty(prop, out var value, out var controlFlags);

                        setting.ControlValue = (ControlFlag)controlFlags;
                        setting.Value = value;
                        setting.VideoProperty = prop;

                        device.GetVideoPropertyRange(
                            prop,
                            out var min,
                            out var max,
                            out var stepSize,
                            out var defaultValue,
                            out var flags
                        );
                        setting.Min = min;
                        setting.Max = max;
                        setting.StepSize = stepSize;
                        setting.Default = defaultValue;
                        setting.ControlRange = (ControlFlag)flags;

                        cam.Settings.Add(setting);
                    }
                    catch (Exception ex) { }
                }
            }

            return cameras;
        }

        public static List<(VideoCaptureDevice device, string name)> GetVideoDevices()
        {
            List<(VideoCaptureDevice device, string name)> cams = [];
            FilterInfoCollection collection = new(FilterCategory.VideoInputDevice);

            cams.AddRange(
                from FilterInfo item in collection
                let cam = new VideoCaptureDevice(item.MonikerString)
                select (cam, item.Name)
            );

            return cams;
        }

        /// returns true if no exception
        /// returns false if threw exception
        public static bool SetCameraPropertyInternal(
            VideoCaptureDevice cam,
            CameraControlProperty prop,
            int value,
            CameraControlFlags controlFlags = CameraControlFlags.Manual
        )
        {
            try
            {
                return cam.SetCameraProperty(prop, value, controlFlags);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED::{prop}::{value} {ex}");
                return false;
            }
        }

        public static bool SetVideoPropertyInternal(
            VideoCaptureDevice cam,
            VideoProcAmpProperty prop,
            int value,
            VideoProcAmpFlags controlFlags = VideoProcAmpFlags.Manual
        )
        {
            try
            {
                return cam.SetVideoProperty(prop, value, controlFlags);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED::{prop}::{value} {ex}");
                return false;
            }
        }
    }
}
