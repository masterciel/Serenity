﻿using System;

namespace Serenity.ComponentModel
{
    /// <summary>
    /// Indicates that this field should have date/time type of filtering
    /// </summary>
    public class DateTimeFilteringAttribute : CustomFilteringAttribute
    {
        /// <summary>
        /// Creates a new DateTimeFilteringAttribute
        /// </summary>
        public DateTimeFilteringAttribute()
            : base("DateTime")
        {
        }

        /// <summary>
        /// Gets/sets optional display format to use for display of filter
        /// </summary>
        public string DisplayFormat
        {
            get { return GetOption<string>("displayFormat"); }
            set { SetOption("displayFormat", value); }
        }
    }
}