using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using InfoHelper.Utils;
using LibVBKernel32;
using LibVBMWCapture;

namespace InfoHelper.DataProcessor
{
    public class CaptureCardManager
    {
        private static IntPtr _hChannel;

        private static IntPtr _hNotifyEvent;
        private static IntPtr _hCaptureEvent;

        private static ulong _hTimerEvent;

        private static readonly int DstStride;

        private static readonly PixelFormat PixelFormat;

        private const int FrameDuration = 400000;

        static CaptureCardManager()
        {
            PixelFormat = PixelFormat.Format24bppRgb;

            int bitsPerPixel = ((int)PixelFormat & 0xff00) >> 8;

            int bytesPerPixel = (bitsPerPixel + 7) / 8;

            DstStride = 4 * ((Shared.ClientWorkSpaceWidth * bytesPerPixel + 3) / 4);
        }

        public void Initialize()
        {
            LibMWCapture.MWCaptureInitInstance();

            int nDeviceIndex = 0;

            int[] path = new int[256];

            GCHandle path_gchandle = GCHandle.Alloc(path, GCHandleType.Pinned);

            IntPtr p_path = path_gchandle.AddrOfPinnedObject();

            LibMWCapture.MWGetDevicePath(nDeviceIndex, p_path);

            _hChannel = LibMWCapture.MWOpenChannelByPath(p_path);

            path_gchandle.Free();

            if (_hChannel == IntPtr.Zero)
                throw new Exception("Unable to open capture card channel");

            _hNotifyEvent = LibKernel32.CreateEvent(IntPtr.Zero, false, false, IntPtr.Zero);
            _hCaptureEvent = LibKernel32.CreateEvent(IntPtr.Zero, false, false, IntPtr.Zero);

            LibMWCapture.MW_RESULT result = LibMWCapture.MWStartVideoCapture(_hChannel, _hCaptureEvent);

            if (result != LibMWCapture.MW_RESULT.MW_SUCCEEDED)
                throw new Exception("Unable to start capturing video");

            _hTimerEvent = LibMWCapture.MWRegisterTimer(_hChannel, _hNotifyEvent);
        }

        public void DisposeResources()
        {
            LibMWCapture.MWUnregisterTimer(_hChannel, _hTimerEvent);

            LibMWCapture.MWStopVideoCapture(_hChannel);

            if (_hChannel != IntPtr.Zero)
                LibMWCapture.MWCloseChannel(_hChannel);

            LibMWCapture.MWCaptureExitInstance();
        }

        public unsafe Bitmap CaptureFrame()
        {
            LibMWCapture.MWCAP_VIDEO_BUFFER_INFO videoBufferInfo = default(LibMWCapture.MWCAP_VIDEO_BUFFER_INFO);
            LibMWCapture.MWGetVideoBufferInfo(_hChannel, ref videoBufferInfo);

            LibMWCapture.MWCAP_VIDEO_FRAME_INFO videoFrameInfo = default(LibMWCapture.MWCAP_VIDEO_FRAME_INFO);
            LibMWCapture.MWGetVideoFrameInfo(_hChannel, videoBufferInfo.iNewestBufferedFullFrame, ref videoFrameInfo);

            LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS videoSignalStatus = default(LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS);
            LibMWCapture.MWGetVideoSignalStatus(_hChannel, ref videoSignalStatus);

            if (videoSignalStatus.state != LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKED)
                throw new Exception("Signal is not in a locked state");

            if (videoSignalStatus.cx != Shared.ClientWorkSpaceWidth || videoSignalStatus.cy != Shared.ClientWorkSpaceHeight + Shared.ClientTaskBarHeight)
                throw new Exception("Client resolution should be 1920x1080");

            ulong llBegin = 0;

            LibMWCapture.MW_RESULT xr = LibMWCapture.MWGetDeviceTime(_hChannel, ref llBegin);

            if (xr != LibMWCapture.MW_RESULT.MW_SUCCEEDED)
                throw new Exception("Unable to get device time");

            xr = LibMWCapture.MWScheduleTimer(_hChannel, _hTimerEvent, llBegin + (ulong)FrameDuration);

            if (xr != LibMWCapture.MW_RESULT.MW_SUCCEEDED)
                throw new Exception("Unable to schedule timer");

            LibKernel32.WaitForSingleObject(_hNotifyEvent, 1000);

            xr = LibMWCapture.MWGetVideoBufferInfo(_hChannel, ref videoBufferInfo);

            if (xr != LibMWCapture.MW_RESULT.MW_SUCCEEDED)
                throw new Exception("Unable to get buffer info");

            MWcap_FOURCC fourcc = new MWcap_FOURCC();

            int cx = videoSignalStatus.cx;
            int cy = videoSignalStatus.cy;

            int frameLength = DstStride * cy;

            IntPtr arrayPointer = Marshal.AllocHGlobal(frameLength);

            Bitmap dstImg = null;

            try
            {
                xr = LibMWCapture.MWCaptureVideoFrameToVirtualAddress(_hChannel, videoBufferInfo.iNewestBufferedFullFrame, arrayPointer, (uint)frameLength, (uint)DstStride, 0, 0, fourcc.MWCAP_FOURCC_BGR24, cx, cy);

                if (xr != LibMWCapture.MW_RESULT.MW_SUCCEEDED)
                    throw new Exception("Unable to capture video frame");

                LibKernel32.WaitForSingleObject(_hCaptureEvent, 1000);

                dstImg = new Bitmap(Shared.ClientWorkSpaceWidth, Shared.ClientWorkSpaceHeight, PixelFormat);

                BitmapData dstImgBitmapData = dstImg.LockBits(new Rectangle(0, 0, dstImg.Width, dstImg.Height), ImageLockMode.WriteOnly, dstImg.PixelFormat);

                byte* dstPtr = (byte*)dstImgBitmapData.Scan0.ToPointer();

                byte* srcPtr = (byte*)arrayPointer.ToPointer();

                WinApi.Memcpy(dstPtr, srcPtr, DstStride * Shared.ClientWorkSpaceHeight);

                dstImg.UnlockBits(dstImgBitmapData);
            }
            finally
            {
                Marshal.FreeHGlobal(arrayPointer);
            }

            return dstImg;
        }
    }
}
