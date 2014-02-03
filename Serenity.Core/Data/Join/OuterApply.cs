
using System.Collections.Generic;
namespace Serenity.Data
{
    public class OuterApply : Join
    {
        public OuterApply(string innerQuery, string alias)
            : base(null, innerQuery.IsEmptyOrNull() ? innerQuery : "(" + innerQuery + ")", alias, null)
        {
        }

        public OuterApply(IDictionary<string, Join> joins, string innerQuery, string alias)
            : base(joins, innerQuery.IsEmptyOrNull() ? innerQuery : "(" + innerQuery + ")", alias, null)
        {
        }

        public override string GetKeyword()
        {
            return "Outer APPLY";
        }
     }
}