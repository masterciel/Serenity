﻿using System.Drawing;

namespace Serenity.Reporting
{
    public interface ICellDecorator
    {
        object Item { get; set; }
        string Name { get; set; }

        object Value { get; set; }
        Color Background { get; set; }
        Color Foreground { get; set; }
        string Format { get; set; }

        void Decorate();
    }
}
