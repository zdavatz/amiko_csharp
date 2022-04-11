/*
Copyright (c) ywesee GmbH

This file is part of AmiKo for Windows.

AmiKo for Windows is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Threading.Tasks;
using System.Windows.Interop;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
//using MediaCaptureWPF.Native;


namespace AmiKoWindows
{
    public class PreviewImage : D3DImage
    {
        //private CapturePreviewNative _Preview;
        private MediaCapture _Capture;
        private uint _Width;
        private uint _Height;

        public PreviewImage(MediaCapture capture)
        {
            var properties = (VideoEncodingProperties)capture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            this._Width = properties.Width;
            this._Height = properties.Height;

            //this._Preview = new CapturePreviewNative(this, _Width, _Height);
            this._Capture = capture;
        }

        public async Task StartAsync()
        {
            var profile = new MediaEncodingProfile
            {
                Audio = null,
                Video = VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Rgb32, _Width, _Height),
                Container = null
            };

            //await _Capture.StartPreviewToCustomSinkAsync(profile, (IMediaExtension)_Preview.MediaSink);
        }

        // Add missing function (MediaCaptureWPF 1.0.0)
        public async Task StopAsync()
        {
            //await _Capture.StopPreviewAsync();
        }
    }
}
