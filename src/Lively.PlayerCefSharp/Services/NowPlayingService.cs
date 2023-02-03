﻿using Lively.PlayerCefSharp.API;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;

namespace Lively.PlayerCefSharp.Services
{
    public sealed class NowPlayingService : INowPlayingService
    {
        private static readonly bool isWindows11_OrGreater = Environment.OSVersion.Version.Build >= 22000;
        public event EventHandler<NowPlayingModel> NowPlayingTrackChanged;
        private readonly NowPlayingModel model = new NowPlayingModel();
        private readonly Timer _timer; //to avoid GC

        public NowPlayingService()
        {
            //There is a MediaPropertiesChanged bug where the event will stop firing after sometime, so using timer instead.
            _timer = new Timer(async (obj) => NowPlayingTrackChanged?.Invoke(this, await GetCurrentTrackInfo()), null, 0, 500);
        }

        private async Task<NowPlayingModel> GetCurrentTrackInfo()
        {
            try
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

                var session = manager.GetCurrentSession();
                if (session is null)
                    return null;

                var media = await session.TryGetMediaPropertiesAsync();
                if (media is null) 
                    return null;

                if (media.Title != model.Title && media.AlbumTitle != model.AlbumTitle)
                {
                    string thumb = null;
                    if (media.Thumbnail != null)
                    {
                        using var ras = await media.Thumbnail.OpenReadAsync();
                        thumb = CreateThumbnail(ras);
                    }

                    model.AlbumArtist = media.AlbumArtist;
                    model.AlbumTitle = media.AlbumTitle;
                    model.AlbumTrackCount = media.AlbumTrackCount;
                    model.Artist = media.Artist;
                    model.Genres = media.Genres?.ToList();
                    model.PlaybackType = media.PlaybackType?.ToString();
                    model.Subtitle = media.Subtitle;
                    model.Thumbnail = thumb;
                    model.Title = media.Title;
                    model.TrackNumber = media.TrackNumber;
                }
                return model;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            return null;
        }

        private static string CreateThumbnail(IRandomAccessStreamWithContentType ras)
        {
            using var stream = ras.AsStream();
            using var ms = new MemoryStream();
            ms.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(ms);
            if (!isWindows11_OrGreater)
            {
                //In Win10 there is transparent borders for some apps
                using var bmp = new Bitmap(ms);
                if (IsPixelAlpha(bmp, 0, 0))
                    return CropImage(bmp, 34, 1, 233, 233);
            }
            var array = ms.ToArray();
            return Convert.ToBase64String(array);
        }

        private static string CropImage(Bitmap bmp, int x, int y, int width, int height)
        {
            var rect = new Rectangle(x, y, width, height);

            using var croppedBitmap = new Bitmap(rect.Width, rect.Height, bmp.PixelFormat);

            var gfx = Graphics.FromImage(croppedBitmap);
            gfx.DrawImage(bmp, 0, 0, rect, GraphicsUnit.Pixel);

            using var ms = new MemoryStream();
            croppedBitmap.Save(ms, ImageFormat.Png);
            byte[] byteImage = ms.ToArray();
            return Convert.ToBase64String(byteImage);
        }

        private static bool IsPixelAlpha(Bitmap bmp, int x, int y) => bmp.GetPixel(x, y).A == (byte)0;
    }
}