using System;

namespace Serenity.Data
{

    /// <summary>
    ///   Bir alan�n temel �zelliklerini belirleyen flag'lar</summary>
    /// <remarks>
    ///   Bilgi ama�l� olarak kullan�ld��� gibi, PagerPanel gibi baz� nesneler taraf�ndan dinamik 
    ///   INSERT, UPDATE sorgular�n�n �retilmesinde de kullan�l�r. �rne�in Insertable olmayan alanlar 
    ///   de�erleri al�nsa da INSERT sorgusuna dahil edilmez. ConvertEmptyStringToNull ise
    ///   INSERT, UPDATE esnas�nda grid ya da details view den gelen string alan de�erlerinin SQL 
    ///   parametrelerine �evrilirken, e�er de�er bo�sa NULL yap�l�p yap�lmayaca��n� belirler.</remarks>
    [Flags]
    public enum FieldFlags
    {
        /// <summary>
        ///   Hi�bir flag set edilmemi�.</summary>
        Internal = 0,
        /// <summary>
        ///   INSERT esnas�nda de�er verilebilir mi? Di�er tablolardan gelen alanlar ve 
        ///   identity gibi alanlar�n bu flag'i olmamal�.</summary>
        Insertable = 1,
        /// <summary>
        ///   UPDATE esnas�nda g�ncellenebilir mi? Di�er tablolardan gelen alanlar ve identity 
        ///   gibi alanlar�n bu flag'i olmamal�.</summary>
        Updatable = 2,
        /// <summary>
        ///   NULL olabilir mi. Gridview ve Detailsview'de validator'lar�n Required �zelliklerinin
        ///   belirlenmesi i�in.</summary>
        NotNull = 4,
        /// <summary>
        ///   Alan anahtar saha ya da sahalardan biri.</summary>
        PrimaryKey = 8,
        /// <summary>
        ///   Otomatik artan integer saha.</summary>
        AutoIncrement = 16,
        /// <summary>
        ///   LEFT OUTER JOIN ile di�er tablolardan gelen alanlar�n bu flag'i set edilmeli.</summary> 
        Foreign = 32,
        /// <summary>
        ///   Hesaplanan alan, Foreign varsa di�er tablolardan da gelen alanlar�n kar���m� olabilir.</summary>
        Calculated = 64,
        /// <summary>
        ///   Just reflects another field value (e.g. negative/absolute of it), so doesn't have client and server side storage of its own,
        ///   and setting it just sets another field.</summary>
        Reflective = 128,
        /// <summary>
        ///   Field which is just a container to use in client side code (might also be client side calculated / reflective).</summary>
        ClientSide = 256,
        /// <summary>
        ///   Trim.</summary>
        Trim = 512,
        /// <summary>
        ///   TrimToEmpty.</summary>
        TrimToEmpty = 512 + 1024,
        /// <summary>
        ///   DenyFiltering.</summary>
        DenyFiltering = 2048,
        /// <summary>
        ///   Yeni bir <see cref="Field"/> �retirken alan �zellikleri belirtilmedi�inde
        ///   kullan�lacak �nde�er �zellikler. Eklenebilir, g�ncellenebilir, NULL yap�labilir,
        ///   bo� string'ler NULL'a �evrilir.</summary>
        Default = Insertable | Updatable | Trim,
        /// <summary>
        ///   Default'tan fark� zorunlu alan olmas�.</summary>
        Required = Default | NotNull,
        /// <summary>
        ///   Otomatik artan anahtar ID alanlar� i�in kullan�lacak flag seti.</summary>
        Identity = PrimaryKey | AutoIncrement | NotNull
    }
}