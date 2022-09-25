using System;
using System.Collections.Generic;
using System.Text;

namespace genshin.relic.score.Models.Recognize
{
    class Character
    {
        public int rarity { get; set; }
        public string character { get; set; }
        public string element { get; set; }
        public string weapon { get; set; }
        public string gender { get; set; }
        public string birthday { get; set; }
        public string country { get; set; }
        public object maxHP { get; set; }
        public int baseATK { get; set; }
        public int DEF { get; set; }
        public int energy { get; set; }
    }
}
