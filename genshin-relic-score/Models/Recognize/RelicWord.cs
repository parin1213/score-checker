using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using genshin.relic.score.JsonConverter;
using Google.Cloud.Vision.V1;
using Newtonsoft.Json;

namespace genshin.relic.score.Models.Recognize
{
    public class RelicWord
    {
        public string text;
        public IEnumerable<RelicWord> chars;

        [JsonConverter(typeof(RectConverter))]
        public Rectangle rect { get; set; }

        [JsonIgnore]
        private readonly List<RelicWord> words = new List<RelicWord>();

        public RelicWord()
        {
            words.Add(this);
        }

        public override string ToString()
        {
            return text + rect.ToString();
        }

        public RelicWord MergeFrom(RelicWord mergedObject, string separator = "+")
        {
            RelicWord newObject = new RelicWord();

            newObject.text = $"{text}{separator}{mergedObject.text}";
            newObject.rect = Rectangle.Union(rect, mergedObject.rect);

            newObject.words.Clear();
            newObject.words.AddRange(words);
            newObject.words.Add(mergedObject);

            return newObject;
        }

        public RelicWord extraWords()
        {
            var candidateWords = text.Split("+").Prepend(text);

            var partWords = words.Where(w => candidateWords.Any(candidateWord => w.text.Contains(candidateWord)));

            var oldRect = this.rect;
            RelicWord newObject = new RelicWord();
            newObject.text = text;
            newObject.rect = partWords.Select(w => w.rect).Aggregate((r1, r2) => Rectangle.Union(r1, r2));
            newObject.words.AddRange(words);
            if(oldRect != newObject.rect)
            {
                var excluds = words.Where(w => candidateWords.Any(candidateWord => w.text.Contains(candidateWord)) == false);
                Console.WriteLine(String.Join(Environment.NewLine, excluds.Select(e => e.ToString())));
            }

            return newObject;
        }
    }
}
