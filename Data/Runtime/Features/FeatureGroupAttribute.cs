using System;

namespace CupkekGames.Data
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class FeatureGroupAttribute : Attribute
    {
        public string Group { get; }
        public int Order { get; set; } = 0;

        public FeatureGroupAttribute(string group)
        {
            Group = group;
        }
    }
}
