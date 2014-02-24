using System;

namespace Serenity.Data.Mapping
{
    public class TableNameAttribute : Attribute
    {
        public TableNameAttribute(string name)
        {
            if (name.IsNullOrEmpty())
                throw new ArgumentNullException("name");

            this.Name = name;
        }

        public string Name { get; private set; }
    }
}