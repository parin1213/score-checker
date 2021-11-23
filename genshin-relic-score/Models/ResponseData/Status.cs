using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using genshin.relic.score.JsonConverter;
using Newtonsoft.Json;

namespace genshin.relic.score.Models.ResponseData
{
    public class Status : IEquatable<Status>
    {
        [JsonIgnore]
        public string Key
        {
            get => pair.Key;
            set => pair = new KeyValuePair<string, double>(value, pair.Value);
        }

        [JsonIgnore]
        public double Value
        {
            get => pair.Value;
            set => pair = new KeyValuePair<string, double>(pair.Key, value);
        }

        public KeyValuePair<string, double> pair;

        [JsonConverter(typeof(RectConverter))]
        public Rectangle rect { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(pair.Key)) { return ""; }

            var key = pair.Key.Replace("%", "");
            var value = pair.Value.ToString("0.##");
            if (pair.Key.Contains("%")) { value = pair.Value.ToString("0.0#") + "%"; }

            return $"{key}+{value}";
        }

        public override bool Equals(object obj) => this.Equals(obj as Status);

        public bool Equals(Status p)
        {
            if (p is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, p))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != p.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (pair.Key == p.pair.Key) && (pair.Value == p.pair.Value) && (rect == default || rect.IntersectsWith(p.rect));
        }

        public override int GetHashCode() => (pair.Key, pair.Value).GetHashCode();

        public static bool operator ==(Status lhs, Status rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Status lhs, Status rhs) => !(lhs == rhs);
    }
}
