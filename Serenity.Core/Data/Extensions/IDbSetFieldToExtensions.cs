﻿using System;

namespace Serenity.Data
{
    /// <summary>
    ///   Extension methods for classes implementing IDbSet interface</summary>
    public static class IDbSetExtensions
    {

        /// <summary>
        ///   Sets a field value with a parameter.</summary>
        /// <param field="field">
        ///   Field name.</param>
        /// <param field="param">
        ///   Parameter name</param>
        /// <param field="value">
        ///   Parameter value</param>
        /// <returns>
        ///   Object itself.</returns>
        public static T Set<T>(this T self, string field, string param, object value) where T : IDbSetFieldTo
        {
            self.SetTo(field, param);
            self.SetParam(param, value);
            return self;
        }

        /// <summary>
        ///   Sets a field value with a parameter.</summary>
        /// <param field="field">
        ///   Field name.</param>
        /// <param field="param">
        ///   Parameter name</param>
        /// <param field="value">
        ///   Parameter value</param>
        /// <returns>
        ///   Object itself.</returns>
        public static T Set<T>(this T self, Field field, string param, object value) where T : IDbSetFieldTo
        {
            self.SetTo(field.Name, param);
            self.SetParam(param, value);
            return self;
        }

        /// <summary>
        ///   Sets a field value with a parameter.</summary>
        /// <param field="field">
        ///   Field name.</param>
        /// <param field="param">
        ///   Parameter name</param>
        /// <param field="value">
        ///   Parameter value</param>
        /// <returns>
        ///   Object itself.</returns>
        public static T Set<T>(this T self, string field, object value) where T : IDbSetFieldTo
        {
            var param = self.AutoParam();
            self.SetTo(field, param.Name);
            self.SetParam(param.Name, value);
            return self;
        }

        /// <summary>
        ///   Sets a field value with a parameter.</summary>
        /// <param field="field">
        ///   Field name.</param>
        /// <param field="param">
        ///   Parameter name</param>
        /// <param field="value">
        ///   Parameter value</param>
        /// <returns>
        ///   Object itself.</returns>
        public static T Set<T>(this T self, Field field, object value) where T : IDbSetFieldTo
        {
            var param = self.AutoParam();
            self.SetTo(field.Name, param.Name);
            self.SetParam(param.Name, value);
            return self;
        }

        /// <summary>
        ///   Sets all field values in a row with auto named parameters (field name prefixed with '@').</summary>
        /// <param field="row">
        ///   The row with modified field values. Must be in TrackAssignments mode, or an exception is raised.</param>
        /// <returns>
        ///   Object itself.</returns>
        public static T Set<T>(this T self, Row row) where T : IDbSetFieldTo
        {
            if (row == null)
                throw new ArgumentNullException("row");
            if (!row.TrackAssignments)
                throw new ArgumentException("row must be in TrackAssignments mode to determine modified fields.");
            foreach (var field in row.GetFields())
                if (row.IsAssigned(field))
                    Set(self, field, field.AsObject(row));
            return self;
        }

        public static T Set<T>(this T self, Field field, out Parameter param) where T : IDbSetFieldTo
        {
            param = self.AutoParam();
            self.SetTo(field.Name, param.Name);
            return self;
        }
    }
}