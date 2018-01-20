﻿using System;
using System.Collections;
using System.Collections.Generic;
using jQueryApi;
using System.Runtime.CompilerServices;

namespace Serenity
{
    [Imported, Serializable, IncludeGenericArguments(false)]
    public class QuickFilter<TWidget, TOptions>
        where TWidget: Widget
    {
        public string Field { get; set; }
        public string Title { get; set; }
        public Type Type { get; set; }
        public TOptions Options { get; set; }
        public Action<jQueryObject> Element { get; set; }
        public Action<TWidget> Init { get; set; }
        public Action<QuickFilterArgs<TWidget>> Handler { get; set; }
        public Func<TWidget, object> SaveState { get; set; }
        public Action<TWidget, object> LoadState { get; set; }
        public Func<TWidget, string, string> DisplayText { get; set; }
        public bool Separator { get; set; }
        public string CssClass { get; set; }
    }
}