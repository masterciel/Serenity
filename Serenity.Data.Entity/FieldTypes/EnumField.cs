﻿using System;
using System.Collections.Generic;

namespace Serenity.Data
{
    public class EnumField<TEnum> : Int32Field
        where TEnum: struct, IComparable, IFormattable, IConvertible
    {
        public EnumField(ICollection<Field> collection, string name, LocalText caption = null, int size = 0, FieldFlags flags = FieldFlags.Default, 
            Func<Row, Int32?> getValue = null, Action<Row, Int32?> setValue = null)
            : base(collection, name, caption, size, flags, getValue, setValue)
        {
            if (!typeof(TEnum).IsEnum)
                throw new InvalidProgramException(typeof(TEnum).FullName + " is used as type parameter for an EnumField but it is not an enum type!");

            if (Enum.GetUnderlyingType(typeof(TEnum)) != typeof(Int32))
                throw new InvalidProgramException(typeof(TEnum).FullName + " is used as type parameter for an EnumField but it is not based on Int32!");

            this.EnumType = typeof(TEnum);
        }

        public new TEnum? this[Row row]
        {
            get
            {
                CheckUnassignedRead(row);
                return (TEnum?)(object)_getValue(row);
            }
            set
            {
                _setValue(row, (Int32?)(object)value);
                if (row.tracking)
                    row.FieldAssignedValue(this);
            }
        }
    }
}
