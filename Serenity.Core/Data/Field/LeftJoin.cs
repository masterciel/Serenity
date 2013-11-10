using System;
using System.Collections.Generic;

namespace Serenity.Data
{
    /// <summary>
    ///   SQL sorgusundaki bir LEFT OUTER JOIN ile ilgili bilgileri tutan Field s�n�f.</summary>
    public class LeftJoin : Alias
    {
        private RowFieldsBase fields;
        private string toTable;
        private string onCriteria;
        private HashSet<string> onCriteriaAliases;

        /// <summary>
        ///   Verilen tablo ad�, alias ve ba�lant� ko�uluna sahip yeni bir LeftJoin olu�turur.</summary>
        /// <param name="toTable">
        ///   Sorguya left outer join ile dahil edilen tablo ad� (zorunlu).</param>
        /// <param name="alias">
        ///   Sorguya left outer join ile dahil edilen tabloya atanan alias (zorunlu).</param>
        /// <param name="onCriteria">
        ///   Left outer join i�leminin "ON(...)" k�sm�nda belirtilen ifade (zorunlu).</param>
        public LeftJoin(RowFieldsBase fields, string toTable, string alias, string onCriteria)
            : base(alias)
        {
            if (toTable == null)
                throw new ArgumentNullException("toTable");

            if (onCriteria == null)
                throw new ArgumentNullException("onCriteria");

            this.fields = fields;
            this.toTable = toTable;
            this.onCriteria = onCriteria.TrimToNull();

            if (onCriteria != null)
            {
                var aliases = JoinAliasLocator.Locate(onCriteria);
                if (aliases.Count > 0)
                    onCriteriaAliases = aliases;
            }

            fields._leftJoins.Add(this.Name, this);
        }

        /// <summary>
        ///   Verilen tablo ad�, alias ve ba�lant� ko�ulu filtresine sahip yeni bir 
        ///   LeftJoin olu�turur.</summary>
        /// <param name="toTable">
        ///   Sorguya left outer join ile dahil edilen tablo ad� (zorunlu).</param>
        /// <param name="alias">
        ///   Sorguya left outer join ile dahil edilen tabloya atanan alias (zorunlu).</param>
        /// <param name="onCriteria">
        ///   Left outer join i�leminin "ON(...)" k�sm�na kar��l�k gelen filtre nesnesi (zorunlu).</param>
        public LeftJoin(RowFieldsBase fields, string toTable, string alias, BaseCriteria onCriteria)
            : this(fields, toTable, alias, onCriteria.ToStringCheckNoParams())
        {
        }

        /// <summary>
        ///   Verilen tablo ad�, join indeksi ve ba�lant� ko�uluna sahip yeni bir 
        ///   LeftJoin olu�turur.</summary>
        /// <param name="toTable">
        ///   Sorguya left outer join ile dahil edilen tablo ad� (zorunlu).</param>
        /// <param name="alias">
        ///   Sorguya left outer join ile dahil edilen tabloya atanacak join indeksi. �r. "1" verilirse join
        ///   alias "T1" olur.</param>
        /// <param name="onCriteria">
        ///   Left outer join i�leminin "ON(...)" k�sm�na kar��l�k gelen filtre nesnesi (zorunlu).</param>
        public LeftJoin(RowFieldsBase fields, string toTable, int alias, BaseCriteria onCriteria)
            : this(fields, toTable, alias.TableAlias(), onCriteria.ToStringCheckNoParams())
        {
        }

        /// <summary>
        ///   Left outer join yap�lan tablo ad�n� verir.</summary>
        public string ToTable
        {
            get
            {
                return toTable;
            }
        }

        /// <summary>
        ///   Left outer join'in "ON(...)" k�sm�nda yaz�lan ifadeyi verir.</summary>
        public string OnCriteria
        {
            get
            {
                return onCriteria;
            }
        }


        /// <summary>
        ///   Left outer join'in "ON(...)" k�sm�nda yaz�lan ifadedeki alias lar�n listesini verir.</summary>
        public HashSet<string> OnCriteriaAliases
        {
            get
            {
                return onCriteriaAliases;
            }
        }

        public RowFieldsBase Fields
        {
            get { return fields; }
        }

    }
}