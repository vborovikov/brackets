namespace Brackets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class AttributeReference
    {
        public AttributeReference(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public bool IsFlag { get; init; }
    }
}