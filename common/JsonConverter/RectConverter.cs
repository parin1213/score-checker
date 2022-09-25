using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace genshin.relic.score.JsonConverter
{
    public class RectConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Rectangle);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var rect = new Rectangle();
            var regex = new Regex("^[\\(]*(?<x>[-]*\\d+),\\s*(?<y>[-]*\\d+),\\s*(?<width>[-]*\\d+),\\s*(?<height>[-]*\\d+)[\\)]*$");
            var value = reader.Value.ToString();
            var match = regex.Match(value);

            var x = match.Groups["x"]?.Value;
            var y = match.Groups["y"]?.Value;
            var width = match.Groups["width"]?.Value;
            var height = match.Groups["height"]?.Value;

            rect.X = int.Parse(x);
            rect.Y = int.Parse(y);
            rect.Width = int.Parse(width);
            rect.Height = int.Parse(height);

            return rect;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var rect = (Rectangle)value;
            writer.WriteValue($"{rect.X}, {rect.Y}, {rect.Width}, {rect.Height}");
        }
    }
}
