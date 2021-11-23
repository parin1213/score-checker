using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using genshin.relic.score.JsonConverter;
using genshin.relic.score.Models.Recognize;
using Newtonsoft.Json;

namespace genshin.relic.score.Models.ResponseData
{
    public class ResponseRelicData :IEquatable<ResponseRelicData>
    {
        public string RelicMD5 { get; set; }
        public List<RelicWord> word_list { get; set; }

        public string score { get; set; }

        public Status main_status { get; set; }

        public List<Status> sub_status { get; set; }

        public string category { get; set; }

        public string set { get; set; }

        public string? StackTrace { get; set; }

        public string? ExceptionMessages { get; set; }

        public string version { get; set; }

        public string req { get; set; }

        [JsonConverter(typeof(RectConverter))]
        public Rectangle cropHint { get; set; }

        public List<ResponseRelicData> extendRelic { get; set; }

#if BLAZOR
        public double scoreNumber 
        {
            get
            { 
                double.TryParse(score, out var _score);
                return _score; 
            }
        }

        public string src { get; set; }

        public bool more { get; set; }

        public bool showDot { get; set; }
#endif
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override bool Equals(object obj) => this.Equals(obj as ResponseRelicData);

        public bool Equals(ResponseRelicData p)
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
            return RelicMD5 == p.RelicMD5;
        }

        public override int GetHashCode() 
        {
            return RelicMD5.GetHashCode();
        }

        public static bool operator ==(ResponseRelicData lhs, ResponseRelicData rhs)
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

        public static bool operator !=(ResponseRelicData lhs, ResponseRelicData rhs) => !(lhs == rhs);

    }
}
