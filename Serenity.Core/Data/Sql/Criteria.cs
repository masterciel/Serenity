
namespace Serenity.Data
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Dictionary = System.Collections.Generic.Dictionary<string, object>;

    /// <summary>
    ///   An object that is used to create criterias by employing operator overloading 
    ///   features of C# language, instead of using string based criterias.</summary>
    [Serializable]
    public class Criteria
    {
        private string criteria;
        private Dictionary<string, object> parameters;

        private const string Quote = "'";
        private const string Quote2 = "''";
        private const string NotLeft = "NOT (";
        private const string Equal = " = ";
        private const string NotEqual = " != ";
        private const string GreaterThan = " > ";
        private const string GreaterThanEqual = " >= ";
        private const string LessThan = " < ";
        private const string LessThanEqual = " <= ";
        private const string And = " AND ";
        private const string Or = " OR ";
        private const string Xor = " XOR ";

        // LanguageID -> Collation name list
        private static Dictionary<Int64, string> _collations = new Dictionary<Int64, string>();

        /// <summary>
        ///   Creates an empty criteria</summary>
        public Criteria()
        {
        }

        /// <summary>
        ///   Creates a new criteria with given condition. This condition is usually a 
        ///   field name, but it can also be a criteria text pre-generated.</summary>
        /// <remarks>
        ///   Usually used like: <c>new Criteria("fieldname") >= 5</c>.</remarks>
        /// <param name="criteria">
        ///   A field name or criteria condition (can be null)</param>
        public Criteria(string criteria)
        {
            this.criteria = criteria;
        }

        /// <summary>
        ///   Creates a new criteria that contains field name of the metafield.</summary>
        /// <param name="field">
        ///   Fieldfield (required).</param>
        public Criteria(Field field)
        {
            if (field == null)
                throw new ArgumentNullException("field");

            this.criteria = field.QueryExpression;
        }

        /// <summary>
        ///   Belirtilen tablo alias'� ve alan ad�n� aralar�na nokta koyarak i�eren yeni bir 
        ///   kriter olu�turur.</summary>
        /// <param name="joinAlias">
        ///   Tablo alias'�. Null ya da bo� olursa �nemsenmez.</param>
        /// <param name="field">
        ///   Alan ad� (zorunlu).</param>
        public Criteria(string joinAlias, string field)
        {
            if (field == null || field.Length == 0)
                throw new ArgumentNullException("field");

            if (joinAlias == null || joinAlias.Length == 0)
                this.criteria = field;
            else
                this.criteria = joinAlias + "." + field;
        }

        /// <summary>
        ///   Belirtilen numerik tablo alias'� (ba��na T konarak) ve alan ad�n� aralar�na 
        ///   nokta koyarak i�eren yeni bir kriter olu�turur.</summary>
        /// <param name="joinNumber">
        ///   Join numaras� (T1 gibi kullan�l�r). De�er s�f�rdan k���kse alan ad� tek ba��na
        ///   kullan�l�r.</param>
        /// <param name="field">
        ///   Alan ad� (zorunlu).</param>
        public Criteria(int joinNumber, string field)
        {
            if (field == null || field.Length == 0)
                throw new ArgumentNullException("field");

            if (joinNumber < 0)
                throw new ArgumentOutOfRangeException("joinNumber");

            this.criteria = joinNumber.TableAliasDot() + field;
        }

        /// <summary>
        ///   Belirtilen join ve alan ad�n� aralar�na nokta koyarak i�eren yeni bir kriter olu�turur.</summary>
        /// <param name="join">
        ///   Tablo alias bilgisini i�eren LeftJoin nesnesi (zorunlu).</param>
        /// <param name="field">
        ///   Alan ad� (zorunlu).</param>
        public Criteria(LeftJoin join, string field)
            : this(join.JoinAlias, field)
        {           
        }

        /// <summary>
        ///   Belirtilen numerik tablo alias'� (ba��na T konarak) ve alan�n ad�n� aralar�na 
        ///   nokta koyarak i�eren yeni bir kriter olu�turur.</summary>
        /// <param name="joinNumber">
        ///   Join numaras� (T1 gibi kullan�l�r)</param>
        /// <param name="field">
        ///   Alan nesnesi (zorunlu).</param>
        public Criteria(Alias alias, Field field)
            : this(alias.Name, field.Name)
        {
        }

        /// <summary>
        ///   Belirtilen numerik tablo alias'� (ba��na T konarak) ve alan�n ad�n� aralar�na 
        ///   nokta koyarak i�eren yeni bir kriter olu�turur.</summary>
        /// <param name="joinNumber">
        ///   Join numaras� (T1 gibi kullan�l�r)</param>
        /// <param name="field">
        ///   Alan nesnesi (zorunlu).</param>
        public Criteria(int joinNumber, Field field)
            : this(joinNumber, field.Name)
        {
        }

        /// <summary>
        ///   Belirtilen join ve meta alan�n ad�n� aralar�na nokta koyarak i�eren yeni bir 
        ///   kriter olu�turur.</summary>
        /// <param name="join">
        ///   Tablo alias bilgisini i�eren LeftJoin nesnesi (zorunlu).</param>
        /// <param name="field">
        ///   Field alan (zorunlu).</param>
        public Criteria(LeftJoin join, Field field)
            : this(join.JoinAlias, field.Name)
        {
        }

        /// <summary>
        ///   Belirtilen join ve meta alan�n ad�n� aralar�na nokta koyarak i�eren yeni bir 
        ///   kriter olu�turur.</summary>
        /// <param name="join">
        ///   Tablo alias bilgisini i�eren LeftJoin nesnesi (zorunlu).</param>
        /// <param name="field">
        ///   Field alan (zorunlu).</param>
        public Criteria(string join, Field field)
            : this(join, field.Name)
        {
        }


        /// <summary>
        ///   Verilen alan ad�n� k��eli parantez i�ine alarak yeni bir kriter olu�turur.
        ///   SQL'de bo�luk i�eren ya da keyword olan alan adlar�n�n kullan�labilmesi 
        ///   i�in gerekebilir.</summary>
        /// <param name="fieldName">
        ///   K��eli parantez i�ine al�n�p kriterye �evrilecek alan ad� (zorunlu).</param>
        /// <returns>
        ///   Alan ad�n� k��eli parantez i�inde i�eren yeni bir kriter.</returns>
        public static Criteria Bracket(string fieldName)
        {
            if (fieldName == null || fieldName.Length == 0)
                throw new ArgumentNullException("fieldName");

            return new Criteria("[" + fieldName + "]");
        }

        private string AddAutoParam(object value)
        {
            var p = Parameter.NextName();
            this.parameters = this.parameters ?? new Dictionary();
            this.parameters.Add(p, value);
            return p;
        }

        public Criteria Clone()
        {
            var clone = new Criteria();
            clone.criteria = this.criteria;
            if (this.parameters != null)
                clone.parameters = new Dictionary(this.parameters);
            return clone;
        }

        private static Criteria CloneWithText(Criteria source, string text)
        {
            var clone = new Criteria(text);
            clone.parameters = source.parameters == null ? null : new Dictionary(source.parameters);
            return clone;
        }

        private static void MergeParameters(Criteria source, Criteria target)
        {
            if (source.parameters == null || source.parameters.Count == 0)
                return;

            if (target.parameters == null)
            {
                target.parameters = new Dictionary(source.parameters);
                return;
            }

            try
            {
                foreach (var pair in source.parameters)
                    target.parameters.Add(pair.Key, pair.Value);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Merged criterias has conflicting keys!", ex);
            }
        }

        /// <summary>
        ///   C#'�n "!" operat�r�n�n Filter nesnesi i�in overload edilmi� hali.</summary>
        /// <param name="criteria">
        ///   Mevcut ko�ulu NOT(...) i�ine al�nacak Filter nesnesi</param>
        /// <returns>
        ///   Filtre nesnesinin i�indeki ko�ulu NOT(...) i�erisine al�p, nesnenin kendisini d�nd�r�r.</returns>
        public static Criteria operator !(Criteria criteria)
        {
            return CloneWithText(criteria, "NOT (" + criteria.criteria + ")");
        }

        /// <summary>
        ///   �ki stringi aralar�na op ile belirtilen ay�rac� da koyarak birle�tirir.</summary>
        /// <remarks>
        ///   <p>�ki string de bo�sa bo� string d�nd�r�l�r.</p>
        ///   <p>�ki string de doluysa aralar�na op ay�rac� konulup yeni bir string d�nd�r�l�r.</p>
        ///   <p>�kisinden biri doluysa dolu olan d�nd�r�l�r.</p>
        /// </remarks>
        /// <param name="str1">
        ///   Birinci string (null olabilir).</param>
        /// <param name="str2">
        ///   �kinci string (null olabilir).</param>
        /// <param name="separator">
        ///   �kisi de doluysa aralar�na koyulacak ay�ra�.</param>
        /// <returns>
        ///   �ki string'den biri ya da aralar�na separator koyularak olu�turulmu� yeni bir string.</returns>
        internal static string JoinIf(string str1, string str2, string separator)
        {
            str1 = str1 ?? String.Empty;
            str2 = str2 ?? String.Empty;

            if (str1.Length == 0 && str2.Length == 0)
                return String.Empty;
            else if (str1.Length != 0)
            {
                if (str2.Length != 0)
                    return str1 + separator + str2;
                else
                    return str1;
            }
            else
                return str2;
        }

        /// <summary>
        ///   �ki kriternin ko�ullar�n� aralar�na op ile belirtilen operat�r� de koyarak birle�tirir ve 
        ///   ikisinden birini d�nd�r�r.</summary>
        /// <remarks>
        ///   <p>�ki kriter de doluysa (ko�ullar� bo� de�ilse), birincinin ko�uluna, ikincinin ko�ulunu araya 
        ///   op koyarak ekler, birinciyi d�nd�r�r.</p>
        ///   <p>Sadece birinci ya da ikincinin ko�ulu doluysa, dolu olan� d�nd�r�r. �kisi de bo�sa 
        ///   birinci d�ner.</p></remarks>
        /// <param name="criteria1">
        ///   Birinci kriter</param>
        /// <param name="criteria2">
        ///   �kinci kriter</param>
        /// <param name="op">
        ///   �kisinin de ko�ullar� doluysa aralar�na koyulacak operat�r. (AND, OR gibi)</param>
        /// <returns>
        ///   �ki kriterden biri (ikisi de doluysa birinci kriternin ko�ulunun sonuna op ay�rac� ve 
        ///   ikincinin ko�ulu eklenir)</returns>
        private static Criteria JoinIf(Criteria criteria1, Criteria criteria2, string op)
        {
            bool criteria1LengthZero = criteria1.IsEmpty;
            bool criteria2LengthZero = criteria2.IsEmpty;
            if (criteria1LengthZero && criteria2LengthZero)
                return new Criteria();
            else if (!criteria1LengthZero)
            {
                if (!criteria2LengthZero)
                {
                    var clone = CloneWithText(criteria1, criteria1.criteria + op + criteria2.criteria);
                    MergeParameters(criteria2, clone);
                    return clone;
                }
                else
                    return criteria1.Clone();
            }
            else
                return criteria2.Clone();
        }

        /// <summary>
        ///   Verilen string bo� de�ilse parantez i�erisine al�r.</summary>
        /// <param name="str">
        ///   Parantez i�ine al�nacak string.</param>
        /// <returns>
        ///   Bo� bir string ya da str'in parantez i�ine al�nm�� hali.</returns>
        internal static string ParantezIf(string str)
        {
            if (str != null && str.Length > 0)
                return "(" + str + ")";
            else
                return str;
        }

        /// <summary>
        ///   Filtrenin sonuna IS NULL ekler.</summary>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria IsNull()
        {
            return CloneWithText(this, this.criteria + " IS NULL");
        }

        /// <summary>
        ///   Filtrenin sonuna IS NOT NULL ekler.</summary>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria IsNotNull()
        {
            return CloneWithText(this, this.criteria + " IS NOT NULL");
        }

        /// <summary>
        ///   Filtrenin ba��na "datepart(part", sonuna ")" ekler.</summary>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria Datepart(string part)
        {
            return CloneWithText(this, "datepart(" + part + "," + this.criteria + ")");
        }

        /// <summary>
        ///   Filtreye LIKE ve verilen bir maskeyi ekler.</summary>
        /// <param name="mask">
        ///   LIKE operat�r� uygulanacak maske (zorunlu).</param>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria Like(string mask)
        {
            if (mask == null)
                throw new ArgumentNullException("mask");

            var clone = this.Clone();
            if (SqlSettings.CurrentDialect.IsCaseSensitive())
            {
                mask = mask.ToUpper();
                clone.criteria = "UPPER(" + this.criteria + ") LIKE " + clone.AddAutoParam(mask);
            }
            else
            {
                clone.criteria = this.criteria + " LIKE " + clone.AddAutoParam(mask);
            }

            return clone;
        }

        /// <summary>
        ///   Filtreye LIKE ve verilen bir maskeyi ekler.</summary>
        /// <param name="mask">
        ///   LIKE operat�r� uygulanacak maske (zorunlu).</param>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria NotLike(string mask)
        {
            if (mask == null)
                throw new ArgumentNullException("mask");

            var clone = this.Clone();
            if (SqlSettings.CurrentDialect.IsCaseSensitive())
            {
                mask = mask.ToUpper();
                clone.criteria = "UPPER(" + this.criteria + ") NOT LIKE " + clone.AddAutoParam(mask);
            }
            else
            {
                clone.criteria = this.criteria + " NOT LIKE " + clone.AddAutoParam(mask);
            }

            return clone;
        }

        /// <summary>
        ///   Filtreye LIKE ve ba��na % koyarak verilen bir maskeyi ekler.</summary>
        /// <param name="mask">
        ///   LIKE operat�r�ne uygulanacak maske. Ba��na % getirilir.</param>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria StartsWith(string mask)
        {
            if (mask == null)
                throw new ArgumentNullException("mask");

            return Like(mask + "%");
        }

        /// <summary>
        ///   Filtreye LIKE ve sonuna % koyarak verilen bir maskeyi ekler.</summary>
        /// <param name="mask">
        ///   LIKE operat�r�ne uygulanacak maske. Sonuna % getirilir.</param>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria EndsWith(string mask)
        {
            if (mask == null)
                throw new ArgumentNullException("mask");

            return Like("%" + mask);
        }

        /// <summary>
        ///   Filtreye LIKE ve ba��na/sonuna % koyarak verilen bir maskeyi ekler.</summary>
        /// <param name="mask">
        ///   LIKE operat�r�ne uygulanacak maske. Ba��na ve sonuna % getirilir.</param>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria Contains(string mask)
        {
            return Like("%" + mask +"%");
        }

        /// <summary>
        ///   Filtreye LIKE ve ba��na/sonuna % koyarak verilen bir maskeyi ekler.</summary>
        /// <param name="mask">
        ///   LIKE operat�r�ne uygulanacak maske. Ba��na ve sonuna % getirilir.</param>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria NotContains(string mask)
        {
            return NotLike("%" + mask + "%");
        }

        /// <summary>
        ///   Filtreye say�sal bir IN (...) ifadesi ekler.</summary>
        /// <param name="values">
        ///   IN(...) k�sm�na eklenecek say� listesi.</param>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria In<T>(params T[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentNullException("values");

            var clone = new Criteria();
            var sb = new StringBuilder(this.criteria);
            sb.Append(" IN (");

            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                    sb.Append(",");

                sb.Append(clone.AddAutoParam(values[i]));
            }

            sb.Append(")");

            clone.criteria = sb.ToString();
            return clone;
        }

        /// <summary>
        ///   Filtreye say�sal bir IN (...) ifadesi ekler.</summary>
        /// <param name="values">
        ///   IN(...) k�sm�na eklenecek say� listesi.</param>
        /// <returns>
        ///   Filter nesnesinin kendisi.</returns>
        public Criteria NotIn<T>(params T[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentNullException("values");

            var clone = new Criteria();
            var sb = new StringBuilder(this.criteria);
            sb.Append(" IN (");

            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                    sb.Append(",");

                sb.Append(clone.AddAutoParam(values[i]));
            }

            sb.Append(")");

            clone.criteria = sb.ToString();
            return clone;
        }


        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve iki kriternin ko�ullar�n� aralar�na "=" koyarak, 
        ///   soldaki kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sol kriter. Sa� kriternin ko�u araya "=" koyularak eklenecek.</param>
        /// <param name="criteria2">
        ///   Sa� kriter.</param>
        /// <returns>
        ///   Sol kriter (criteria1)</returns>
        public static Criteria operator ==(Criteria criteria1, Criteria criteria2)
        {
            var clone = CloneWithText(criteria1, criteria1.criteria + Equal + criteria2.criteria);
            MergeParameters(criteria2, clone);
            return clone;
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "=" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator ==(Criteria criteria1, Parameter param)
        {
            return CloneWithText(criteria1, criteria1.criteria + Equal + param.Name);
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "=" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator ==(Criteria criteria1, int value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + Equal + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, Int64 de�erini aralar�na "=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "=" ve Int64 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int64 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator ==(Criteria criteria1, Int64 value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + Equal + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, String de�erini aralar�na "=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "=" ve String de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator ==(Criteria criteria1, string value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + Equal + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, Double de�erini aralar�na "=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "=" ve Double de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator ==(Criteria criteria1, Double value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + Equal + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, Decimal de�erini aralar�na "=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "=" ve Decimal de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator ==(Criteria criteria1, Decimal value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + Equal + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, DateTime de�erini aralar�na "=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "=" ve DateTime de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator ==(Criteria criteria1, DateTime value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + Equal + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, Guid de�erini aralar�na "=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "=" ve Guid de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator ==(Criteria criteria1, Guid value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + Equal + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "!=" operat�r�n� overload eder ve iki kriternin ko�ullar�n� aralar�na "&lt;&gt;" koyarak, 
        ///   soldaki kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sol kriter. Sa� kriternin ko�u araya "=" koyularak eklenecek.</param>
        /// <param name="criteria2">
        ///   Sa� kriter.</param>
        /// <returns>
        ///   Sol kriter (criteria1)</returns>
        public static Criteria operator !=(Criteria criteria1, Criteria criteria2)
        {
            var clone = CloneWithText(criteria1, criteria1.criteria + NotEqual + criteria2.criteria);
            MergeParameters(criteria2, clone);
            return clone;
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "!=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "!=" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator !=(Criteria criteria1, Parameter param)
        {
            return CloneWithText(criteria1, criteria1.criteria + NotEqual + param.Name);
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "!=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "!=" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator !=(Criteria criteria1, Int32 value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + NotEqual + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "==" operat�r�n� overload eder ve bir kriterle, Int64 de�erini aralar�na "!=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "!=" ve Int64 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int64 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator !=(Criteria criteria1, Int64 value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + NotEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "!=" operat�r�n� overload eder ve bir kriterle, String de�erini aralar�na "!=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "!=" ve String de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator !=(Criteria criteria1, string value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + NotEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "!=" operat�r�n� overload eder ve bir kriterle, Double de�erini aralar�na "!=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "!=" ve Double de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator !=(Criteria criteria1, Double value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + NotEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "!=" operat�r�n� overload eder ve bir kriterle, Decimal de�erini aralar�na "!=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "!=" ve Decimal de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator !=(Criteria criteria1, Decimal value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + NotEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "!=" operat�r�n� overload eder ve bir kriterle, DateTime de�erini aralar�na "!=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "!=" ve DateTime de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator !=(Criteria criteria1, DateTime value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + NotEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "!=" operat�r�n� overload eder ve bir kriterle, Guid de�erini aralar�na "!=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "!=" ve Guid de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator !=(Criteria criteria1, Guid value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + NotEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "&gt;" operat�r�n� overload eder ve iki kriternin ko�ullar�n� aralar�na "&gt;" koyarak, 
        ///   soldaki kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sol kriter. Sa� kriternin ko�u araya "&gt;" koyularak eklenecek.</param>
        /// <param name="criteria2">
        ///   Sa� kriter.</param>
        /// <returns>
        ///   Sol kriter (criteria1)</returns>
        public static Criteria operator >(Criteria criteria1, Criteria criteria2)
        {
            var clone = CloneWithText(criteria1, criteria1.criteria + GreaterThanEqual + criteria2.criteria);
            MergeParameters(criteria2, clone);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "&gt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >(Criteria criteria1, Parameter param)
        {
            return CloneWithText(criteria1, criteria1.criteria + GreaterThan + param.Name);
        }

        /// <summary>
        ///   C#'�n "&gt;" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "&gt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >(Criteria criteria1, Int32 value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;" operat�r�n� overload eder ve bir kriterle, Int64 de�erini aralar�na "&gt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;" ve Int64 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int64 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >(Criteria criteria1, Int64 value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;" operat�r�n� overload eder ve bir kriterle, String de�erini aralar�na "&gt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;" ve String de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >(Criteria criteria1, string value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;" operat�r�n� overload eder ve bir kriterle, Double de�erini aralar�na "&gt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;" ve Double de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >(Criteria criteria1, Double value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;" operat�r�n� overload eder ve bir kriterle, Decimal de�erini aralar�na "&gt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;" ve Decimal de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >(Criteria criteria1, Decimal value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;" operat�r�n� overload eder ve bir kriterle, DateTime de�erini aralar�na "&gt;" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;" ve DateTime de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >(Criteria criteria1, DateTime value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;" operat�r�n� overload eder ve bir kriterle, Guid de�erini aralar�na "&gt;" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;" ve Guid de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >(Criteria criteria1, Guid value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;=" operat�r�n� overload eder ve iki kriternin ko�ullar�n� aralar�na "&gt;=" koyarak, 
        ///   soldaki kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sol kriter. Sa� kriternin ko�u araya "&gt;=" koyularak eklenecek.</param>
        /// <param name="criteria2">
        ///   Sa� kriter.</param>
        /// <returns>
        ///   Sol kriter (criteria1)</returns>
        public static Criteria operator >=(Criteria criteria1, Criteria criteria2)
        {
            var clone = CloneWithText(criteria1, criteria1.criteria + GreaterThanEqual + criteria2.criteria);
            MergeParameters(criteria2, clone);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;=" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "&gt;=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;=" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >=(Criteria criteria1, Parameter param)
        {
            return CloneWithText(criteria1, criteria1.criteria + GreaterThanEqual + param.Name);
        }

        /// <summary>
        ///   C#'�n "&gt;=" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "&gt;=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;=" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >=(Criteria criteria1, int value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThanEqual + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&gt;=" operat�r�n� overload eder ve bir kriterle, Int64 de�erini aralar�na "&gt;=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;=" ve Int64 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int64 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >=(Criteria criteria1, long value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThanEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "&gt;=" operat�r�n� overload eder ve bir kriterle, String de�erini aralar�na "&gt;=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;=" ve String de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >=(Criteria criteria1, string value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThanEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "&gt;=" operat�r�n� overload eder ve bir kriterle, Double de�erini aralar�na "&gt;=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;=" ve Double de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >=(Criteria criteria1, double value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThanEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "&gt;=" operat�r�n� overload eder ve bir kriterle, Decimal de�erini aralar�na "&gt;=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;=" ve Decimal de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >=(Criteria criteria1, decimal value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThanEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "&gt;=" operat�r�n� overload eder ve bir kriterle, DateTime de�erini aralar�na "&gt;=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;=" ve DateTime de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >=(Criteria criteria1, DateTime value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThanEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "&gt;=" operat�r�n� overload eder ve bir kriterle, Guid de�erini aralar�na "&gt;=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&gt;=" ve Guid de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator >=(Criteria criteria1, Guid value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + GreaterThanEqual + clone.AddAutoParam(value);
            return clone;

        }

        /// <summary>
        ///   C#'�n "&lt;" operat�r�n� overload eder ve iki kriternin ko�ullar�n� aralar�na "&lt;" koyarak, 
        ///   soldaki kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sol kriter. Sa� kriternin ko�u araya "&lt;" koyularak eklenecek.</param>
        /// <param name="criteria2">
        ///   Sa� kriter.</param>
        /// <returns>
        ///   Sol kriter (criteria1)</returns>
        public static Criteria operator <(Criteria criteria1, Criteria criteria2)
        {
            var clone = CloneWithText(criteria1, criteria1.criteria + LessThan + criteria2.criteria);
            MergeParameters(criteria2, clone);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "&lt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <(Criteria criteria1, Parameter param)
        {
            return CloneWithText(criteria1, criteria1.criteria + LessThan + param.Name);
        }

        /// <summary>
        ///   C#'�n "&lt;" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "&lt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <(Criteria criteria1, int value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;" operat�r�n� overload eder ve bir kriterle, Int64 de�erini aralar�na "&lt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;" ve Int64 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int64 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <(Criteria criteria1, long value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;" operat�r�n� overload eder ve bir kriterle, String de�erini aralar�na "&lt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;" ve String de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <(Criteria criteria1, string value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;" operat�r�n� overload eder ve bir kriterle, Double de�erini aralar�na "&lt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;" ve Double de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <(Criteria criteria1, double value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;" operat�r�n� overload eder ve bir kriterle, Double de�erini aralar�na "&lt;" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;" ve Double de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <(Criteria criteria1, decimal value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;" operat�r�n� overload eder ve bir kriterle, DateTime de�erini aralar�na "&lt;" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;" ve DateTime de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <(Criteria criteria1, DateTime value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;" operat�r�n� overload eder ve bir kriterle, Guid de�erini aralar�na "&lt;" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;" ve Guid de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <(Criteria criteria1, Guid value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThan + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;=" operat�r�n� overload eder ve iki kriternin ko�ullar�n� aralar�na "&lt;=" koyarak, 
        ///   soldaki kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sol kriter. Sa� kriternin ko�u araya "&lt;=" koyularak eklenecek.</param>
        /// <param name="criteria2">
        ///   Sa� kriter.</param>
        /// <returns>
        ///   Sol kriter (criteria1)</returns>
        public static Criteria operator <=(Criteria criteria1, Criteria criteria2)
        {
            var clone = CloneWithText(criteria1, criteria1.criteria + LessThanEqual + criteria2.criteria);
            MergeParameters(criteria2, clone);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;=" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "&lt;=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;=" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <=(Criteria criteria1, Parameter param)
        {
            return CloneWithText(criteria1, criteria1.criteria + LessThanEqual + param.Name);
        }

         /// <summary>
        ///   C#'�n "&lt;=" operat�r�n� overload eder ve bir kriterle, Int32 de�erini aralar�na "&lt;=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;=" ve Int32 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int32 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <=(Criteria criteria1, int value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThanEqual + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;=" operat�r�n� overload eder ve bir kriterle, Int64 de�erini aralar�na "&lt;=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;=" ve Int64 de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   Int64 tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <=(Criteria criteria1, Int64 value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThanEqual + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;=" operat�r�n� overload eder ve bir kriterle, String de�erini aralar�na "&lt;=" koyarak, 
        ///   kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;=" ve String de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <=(Criteria criteria1, string value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThanEqual + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;=" operat�r�n� overload eder ve bir kriterle, Double de�erini aralar�na "&lt;=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;=" ve Double de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <=(Criteria criteria1, double value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThanEqual + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;=" operat�r�n� overload eder ve bir kriterle, decimal de�erini aralar�na "&lt;=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;=" ve decimal de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <=(Criteria criteria1, decimal value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThanEqual + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   C#'�n "&lt;=" operat�r�n� overload eder ve bir kriterle, DateTime de�erini aralar�na "&lt;=" 
        ///   koyarak, kriter i�erisinde birle�tirir.</summary>
        /// <param name="criteria1">
        ///   Sonuna "&lt;=" ve DateTime de�eri SQL sabitine �evirilip eklenecek kriter.</param>
        /// <param name="value">
        ///   String tipinde de�er.</param>
        /// <returns>
        ///   Filter nesnesi (criteria1)</returns>
        public static Criteria operator <=(Criteria criteria1, DateTime value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThanEqual + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   Overloads C#'s "&lt;=" operator and joins a criteria and a value by putting a "&lt;=" between in 
        ///   left criteria.</summary>
        /// <param name="criteria1">
        ///   Filter.</param>
        /// <param name="value">
        ///   Value.</param>
        /// <returns>
        ///   criteria1</returns>
        public static Criteria operator <=(Criteria criteria1, Guid value)
        {
            var clone = criteria1.Clone();
            clone.criteria = criteria1.criteria + LessThanEqual + clone.AddAutoParam(value);
            return clone;
        }

        /// <summary>
        ///   Overloads C#'s "&amp;" operator and joins two criterias by putting a "AND" between in 
        ///   left criteria.</summary>
        /// <param name="criteria1">
        ///   Filter 1.</param>
        /// <param name="criteria2">
        ///   Filter 2.</param>
        /// <returns>
        ///   criteria1</returns>
        public static Criteria operator &(Criteria criteria1, Criteria criteria2)
        {
            return JoinIf(criteria1, criteria2, And);
        }

        /// <summary>
        ///   Overloads C#'s "^" operator and joins two criterias by putting a "OR" between in 
        ///   left criteria.</summary>
        /// <param name="criteria1">
        ///   Filter 1.</param>
        /// <param name="criteria2">
        ///   Filter 2.</param>
        /// <returns>
        ///   criteria1</returns>
        public static Criteria operator |(Criteria criteria1, Criteria criteria2)
        {
            return JoinIf(criteria1, criteria2, Or);
        }

        /// <summary>
        ///   Overloads C#'s "^" operator and joins two criterias by putting a "XOR" between in 
        ///   left criteria.</summary>
        /// <param name="criteria1">
        ///   Filter 1.</param>
        /// <param name="criteria2">
        ///   Filter 2.</param>
        /// <returns>
        ///   criteria1</returns>
        public static Criteria operator ^(Criteria criteria1, Criteria criteria2)
        {
            return JoinIf(criteria1, criteria2, Xor);
        }

        /// <summary>
        ///   Overloads C#'s "~" operator and puts criteria in paranthesis.</summary>
        /// <param name="criteria">
        ///   Filter.</param>
        /// <returns>
        ///   criteria in paranthesis</returns>
        public static Criteria operator ~(Criteria criteria)
        {
            if (!criteria.IsEmpty)
            {
                return CloneWithText(criteria, "(" + criteria.criteria + ")");
            }
            else
                return criteria.Clone();
        }

        /// <summary>
        ///   Adds "COLLATION " text and name of the collation specified to the criteria.</summary>
        /// <param name="collation">
        ///   Collation name (can be null or empty, if so nothing changes)</param>
        /// <returns>
        ///   Filter object itself.</returns>
        public Criteria Collate(string collation)
        {
            if (collation != null && collation.Length > 0)
            {
                return CloneWithText(this, this.criteria + " COLLATE " + collation);
            }
            return this.Clone();
        }

        /// <summary>
        ///   Adds "COLLATION " text and name of the collation matching a language ID specified to the criteria.</summary>
        /// <param name="languageID">
        ///   Language ID</param>
        /// <returns>
        ///   Filter object itself.</returns>
        public Criteria Collate(Int64 languageID)
        {
            return Collate(GetCollation(languageID));
        }

        /// <summary>
        ///   Gets matching collation name for a language ID.</summary>
        /// <param name="languageID">
        ///   Language ID.</param>
        /// <returns>
        ///   Collation name. If not found, null.</returns>
        /// <seealso cref="SetCollations"/>
        public static string GetCollation(Int64 languageID)
        {
            string collation;
            if (_collations.TryGetValue(languageID, out collation))
                return collation;
            else
                return null;
        }

        /// <summary>
        ///   Sets the language ID -&gt; Collection name dictionary.</summary>
        /// <param name="collations">
        ///   LanguageID -&gt; Collation dictionary.</param>
        public static void SetCollations(IDictionary<Int64, string> collations)
        {
            if (collations == null)
                throw new ArgumentNullException("sqlCollations");

            Dictionary<Int64, string> newCollations = new Dictionary<Int64, string>();
            foreach (var p in collations)
                newCollations.Add(p.Key, p.Value);
            _collations = newCollations;
        }

        /// <summary>
        ///   Compares this criteria with another object.</summary>
        /// <param name="obj">
        ///   Object to compare to.</param>
        /// <returns>
        ///   If object is of type Filter and its condition is equal, True.</returns>
        public override bool Equals(object obj)
        {
            return (obj is Criteria && ((Criteria)obj).criteria.Equals(this.criteria));
        }

        /// <summary>
        ///   Creates a new EXISTS criteria</summary>
        /// <param name="expression">
        ///   Expression</param>
        /// <returns></returns>
        public static Criteria Exists(string expression)
        {
            return new Criteria("EXISTS (" + expression + ")");
        }

        /// <summary>
        ///   Returns an hashcode for the criteria.</summary>
        /// <returns>
        ///   Hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public IDictionary<string, object> Parameters
        {
            get { return parameters; }
        }

        /// <summary>
        ///   Gets if criteria is empty.</summary>
        public bool IsEmpty
        {
            get 
            {
                return criteria.IsEmptyOrNull();
            }
        }

        /// <summary>
        ///   Converts criteria to a string.</summary>
        /// <returns>
        ///   Filter condition as string.</returns>
        public override string ToString()
        {
            return (criteria ?? "").ToString();
        }
    }
}