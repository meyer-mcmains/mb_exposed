using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Nancy;
using ThumbnailSharp;
using Utf8Json;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public class GetArtwork : NancyModule
    {
        public GetArtwork()
        {
            Get["/artwork"] = _ =>
            {
                Plugin.MusicBeeApiInterface mbApi = MbApiInstance.Instance.MusicBeeApiInterface;
                PictureLocations pictureLocations = PictureLocations.None;
                string[] album = null;
                mbApi.Library_QueryFilesEx($"Artist={Request.Query.artist}\0Album={Request.Query.album}", ref album);
                string pictureUrl = null;
                byte[] image = null;
                mbApi.Library_GetArtworkEx(album[0], 0, true, ref pictureLocations, ref pictureUrl, ref image);

                if (Request.Query.thumbnail)
                {
                    try
                    {
                        byte[] thumbnail = new ThumbnailCreator().CreateThumbnailBytes(thumbnailSize: 400, imageBytes: image, imageFormat: Format.Jpeg);
                        string color = GetColor(thumbnail);
                        return WriteJson(Convert.ToBase64String(thumbnail), false, color);
                    }
                    catch (Exception e)
                    {
                        if (e.Message == "Thumbnail size must be less than image's size")
                        {
                            string color = GetColor(image);
                            return WriteJson(Convert.ToBase64String(image), false, color);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return Convert.ToBase64String(image);
                }
            };
        }

        private string WriteJson(string image, bool isDark, string color)
        {
            JsonWriter writer = new JsonWriter();
            writer.WriteBeginObject();
            writer.WritePropertyName("data");
            writer.WriteString(image);
            writer.WriteValueSeparator();
            writer.WritePropertyName("isDark");
            writer.WriteBoolean(isDark);
            writer.WriteValueSeparator();
            writer.WritePropertyName("color");
            writer.WriteString(color);
            writer.WriteEndObject();
            return writer.ToString();
        }

        private string GetColor(byte[] image)
        {
            // Lock Bits would be quicker
            TypeConverter tc = TypeDescriptor.GetConverter(typeof(Bitmap));
            Bitmap bitmap = (Bitmap)tc.ConvertFrom(image);
            using (bitmap)
            {
                List<Color> colors = new List<Color>(bitmap.Width * bitmap.Height);
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        colors.Add(bitmap.GetPixel(x, y));
                    }
                }

                List<Color> filtered = colors
                      .Where(c => (KCluster.EuclideanDistance(c, Color.Black) >= 200) && (KCluster.EuclideanDistance(c, Color.White) >= 200))
                      .ToList();

                KMeansClusteringCalculator clustering = new KMeansClusteringCalculator();
                IList<Color> dominantColours = clustering.Calculate(10, filtered, 20.0d);

                int index = closestColor(dominantColours.Skip(1).ToList(), dominantColours.First());
                if (index == 0)
                {
                    return ToRGBAString(dominantColours[2]);
                }
                return ToRGBAString(dominantColours[1]);
            }
        }

        private string ToRGBAString(Color c)
        {
            return $"rgba({c.R},{c.G},{c.B},1)";
        }

        private int closestColor(IList<Color> colors, Color target)
        {
            var hue1 = target.GetHue();
            var diffs = colors.Select(n => getHueDistance(n.GetHue(), hue1));
            var diffMin = diffs.Min(n => n);
            return diffs.ToList().FindIndex(n => n == diffMin);
        }

        // distance between two hues:
        private float getHueDistance(float hue1, float hue2)
        {
            float d = Math.Abs(hue1 - hue2); return d > 180 ? 360 - d : d;
        }
    }
}