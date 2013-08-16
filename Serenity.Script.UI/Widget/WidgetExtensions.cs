﻿using jQueryApi;
using System;

namespace Serenity
{
    public static class WidgetExtensions
    {
        public static TWidget GetWidget<TWidget>(this jQueryObject element) where TWidget : Widget
        {
            var widget = TryGetWidget<TWidget>(element);
            if (widget == null)
                throw new Exception(String.Format("Element has no widget of type '{0}'!", GetWidgetName(typeof(TWidget))));

            return widget;
        }

        public static object GetWidget(this jQueryObject element, Type widgetType)
        {
            var widget = TryGetWidget(element, widgetType);
            if (widget == null)
                throw new Exception(String.Format("Element has no widget of type '{0}'!", GetWidgetName(widgetType)));

            return widget;
        }

        public static TWidget TryGetWidget<TWidget>(this jQueryObject element) where TWidget : Widget
        {
            if (element == null)
                throw new Exception("Argument 'element' is null!");

            var widgetName = WidgetExtensions.GetWidgetName(typeof(TWidget));

            return element.GetDataValue(widgetName) as TWidget;
        }

        public static object TryGetWidget(this jQueryObject element, Type widgetType)
        {
            if (widgetType == null)
                throw new Exception("Argument 'widgetType' is null!");

            if (element == null)
                throw new Exception("Argument 'element' is null!");

            var widgetName = WidgetExtensions.GetWidgetName(widgetType);
            var widget = element.GetDataValue(widgetName);
            
            if (widget != null && !widgetType.IsAssignableFrom(widget.GetType()))
                return null;

            return widget;
        }

        public static string GetWidgetName(Type type)
        {
            return type.FullName.Replace(".", "_");
        }

        public static bool HasOriginalEvent(jQueryEvent e)
        {
            return !Script.IsUndefined(((dynamic)e).originalEvent);
        }
    }
}
