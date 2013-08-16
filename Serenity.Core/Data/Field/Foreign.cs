using System;

namespace Serenity.Data
{
    /// <summary>
    ///   Bir kayd�n join'lerden gelen alanlar� i�in kullan�labilecek, ba�l� oldu�u join'i ve kaynak
    ///   expression'�n� i�inde saklayan Field alan s�n�f�</summary>
    public struct Foreign
    {
        /// <summary>
        ///   kaynak join, "T0" gibi, birden fazla join alan�n�n kar���m�ndan elde edilen,
        ///   �r. "T0.X + T1.Y" gibi alanlar i�in null olabilir</summary>
        private string _joinAlias;
        /// <summary>
        ///   kaynak ifade, source join d���ndaki ifadeyi i�erir. �r. alan�n ifadesi "T0.KOD" ise
        ///   expression "KOD" olmal�. Alan ad�yla ifade ayn� oldu�unda bu saha null olabilir.</summary>
        private string _joinField;

        ///// <summary>
        /////   Verilen kaynak join, kaynak alan ve alias'a sahip yeni bir Foreign nesnesi 
        /////   olu�turur.</summary>
        ///// <param name="sourceJoin">
        /////   Kaynak join. "T0" gibi. Null olamaz.</param>
        ///// <param name="sourceField">
        /////   Kaynak alan. "T0.KOD" i�in "KOD" ge�irilmeli. Null olamaz.</param>
        ///// <param name="name">
        /////   Alana select sorgular�nda atanacak alias. Null olamaz.</param>
        //public Foreign(string joinAlias, string joinField)
        //{
        //    if (joinAlias == null)
        //        throw new ArgumentNullException("joinAlias");
        //    if (joinField == null)
        //        throw new ArgumentNullException("joinField");
        //    _joinAlias = joinAlias;
        //    _joinField = joinField;
        //}

        public Foreign(LeftJoin join, string joinField)
        {
            if (join == null)
                throw new ArgumentNullException("join");
            if (joinField == null)
                throw new ArgumentNullException("joinField");
            _joinAlias = join.JoinAlias;
            _joinField = joinField;
        }

        /// <summary>
        ///   Alan�n olu�turulmas� s�ras�nda atanan kaynak join'i verir ("T0" gibi).
        ///   Birden fazla join tablosundan hesaplanan karma��k alanlar i�in null olabilir.</summary>
        public string JoinAlias
        {
            get { return _joinAlias; }
        }

        /// <summary>
        ///   Alan�n olu�turulmas� s�ras�nda atanan ifadeyi verir. ("KOD" gibi).
        ///   Bu ifade null ise alan ad� ile ifade ayn� demektir. �fade kullan�l�rken
        ///   SourceJoin ile birlikte de�erlendirilmelidir.</summary>
        public string JoinField
        {
            get { return _joinField; }
        }
    }
}