using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using UnityEngine;

namespace PeasAPI
{
    public static class Utility
    {
        public static Sprite CreateSprite(string image, float pixelsPerUnit = 128f)
        {
            Texture2D tex = CanvasUtilities.CreateEmptyTexture();
            Stream myStream = Assembly.GetCallingAssembly().GetManifestResourceStream(image);
            byte[] data = myStream.ReadFully();
            ImageConversion.LoadImage(tex, data, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            sprite.DontDestroy();
            return sprite;
        }

        public static string ColorString(Color c, string s)
        {
            return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>{4}</color>", ToByte(c.r), ToByte(c.g), ToByte(c.b),
                ToByte(c.a), s);
        }

        private static byte ToByte(float f)
        {
            f = Mathf.Clamp01(f);
            return (byte)(f * 255);
        }
        
        public static List<PlayerControl> GetAllPlayers()
        {
            if (PlayerControl.AllPlayerControls != null && PlayerControl.AllPlayerControls.Count > 0)
                return PlayerControl.AllPlayerControls.ToArray().ToList();
            return GameData.Instance.AllPlayers.ToArray().ToList().ConvertAll(p => p.Object);
        }

        public class StringColor
        {
            public const string Reset = "<color=#ffffffff>";
            public const string White = "<color=#ffffffff>";
            public const string Black = "<color=#000000ff>";
            public const string Red = "<color=#ff0000ff>";
            public const string Green = "<color=#169116ff>";
            public const string Blue = "<color=#0400ffff>";
            public const string Yellow = "<color=#f5e90cff>";
            public const string Purple = "<color=#a600ffff>";
            public const string Cyan = "<color=#00fff2ff>";
            public const string Pink = "<color=#e34dd4ff>";
            public const string Orange = "<color=#ff8c00ff>";
            public const string Brown = "<color=#8c5108ff>";
            public const string Lime = "<color=#1eff00ff>";
        }
    }
}