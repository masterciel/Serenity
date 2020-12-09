﻿namespace Serenity.ComponentModel
{
    /// <summary>
    /// Sets filtering type as "Date"
    /// </summary>
    /// <seealso cref="Serenity.ComponentModel.CustomFilteringAttribute" />
    public class DateFilteringAttribute : CustomFilteringAttribute
    {
        /// <summary>Initializes a new instance of the <see cref="DateFilteringAttribute"/> class.</summary>
        public DateFilteringAttribute()
            : base("Date")
        {
        }

        /// <summary>
        /// Gets or sets the display format.
        /// </summary>
        /// <value>
        /// The display format.
        /// </value>
        public string DisplayFormat
        {
            get { return GetOption<string>("displayFormat"); }
            set { SetOption("displayFormat", value); }
        }
    }
}