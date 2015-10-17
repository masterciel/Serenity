﻿using Serenity.ComponentModel;
using Serenity.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Serenity.PropertyGrid
{
    public partial class BasicPropertyProcessor : PropertyProcessor
    {
        private void SetWidth(IPropertySource source, PropertyItem item)
        {
            var widthAttr = source.GetAttribute<WidthAttribute>();
            var basedOnField = source.BasedOnField;
            item.Width = widthAttr == null ? (!ReferenceEquals(null, basedOnField) ? AutoWidth(basedOnField) : 80) : widthAttr.Value;
            item.MinWidth = widthAttr == null ? 0 : widthAttr.Min;
            item.MaxWidth = widthAttr == null ? 0 : widthAttr.Max;
        }

        private static int AutoWidth(Field field)
        {
            var name = field.Name;

            switch (field.Type)
            {
                case FieldType.String:
                    if (field.Size != 0 && field.Size <= 25)
                        return Math.Max(field.Size * 6, 150);
                    else if (field.Size == 0)
                        return 250;
                    else
                        return 150;
                case FieldType.Boolean:
                    return 40;
                case FieldType.DateTime:
                    return 85;
                case FieldType.Int16:
                    return 55;
                case FieldType.Int32:
                    return 65;
                case FieldType.Double:
                case FieldType.Decimal:
                    return 85;
                default:
                    return 80;
            }
        }
    }
}