using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using genshin.relic.score.JsonConverter;
using Newtonsoft.Json;

namespace genshin.relic.score.Models.Recognize
{
    public class RelicWord
    {
        public string text;

        [JsonConverter(typeof(RectConverter))]
        public Rectangle rect { get; set; }

        public override string ToString()
        {
            return text;
        }

        public RelicWord MergeFrom(RelicWord mergedObject)
        {
            RelicWord newObject = new RelicWord();

            newObject.text = $"{text}+{mergedObject.text}";
            newObject.rect = Rectangle.Union(rect, mergedObject.rect);
            
            return newObject;
        }
    }
}
