﻿using jQueryApi;
using System;

namespace Serenity
{
    public static class SlickFormatting
    {
        public static string GetEnumText<TEnum>(this TEnum value)
            where TEnum: struct
        {
            return EnumFormatter.GetText<TEnum>(value);
        }

        public static string GetEnumText(string enumKey, string name)
        {
            return EnumFormatter.GetText(enumKey, name);
        }

        [Obsolete("Use EnumFormatter. This might not function properly!")]
        public static SlickFormatter Enum(string enumKey)
        {
            return delegate(SlickFormatterContext ctx)
            {
                if (Script.IsValue(ctx.Value))
                    return Q.HtmlEncode(Q.Text("Enums." + enumKey + "." + ctx.Value));
                else
                    return "";
            };
        }

        public static SlickFormatter TreeToggle<TEntity>(Func<SlickRemoteView<TEntity>> getView, Func<TEntity, object> getId, 
            SlickFormatter formatter)
        {
            return delegate(SlickFormatterContext ctx)
            {
                var text = formatter(ctx);
                var view = getView();
                var indent = (((object)ctx.Item._indent).As<Int32?>() ?? 0);
                var spacer = "<span class=\"s-TreeIndent\" style=\"width:" + (15 * (indent)) + "px\"></span>";
                var id = getId(ctx.Item);
                var idx = view.GetIdxById(id);
                var next = view.GetItemByIdx(idx + 1);
                
                if (next != null)
                {
                    var nextIndent = ((object)(((dynamic)next)._indent)).As<Int32?>() ?? 0;
                    if (nextIndent > indent)
                    {
                        if (Q.IsTrue(ctx.Item._collapsed))
                            return spacer + "<span class=\"s-TreeToggle s-TreeExpand\"></span>" + text;
                        else
                            return spacer + "<span class=\"s-TreeToggle s-TreeCollapse\"></span>" + text;
                    }
                }

                return spacer + "<span class=\"s-TreeToggle\"></span>" + text;
            };
        }

        public static SlickFormatter Date(string format = null)
        {
            format = format ?? Q.Culture.DateFormat;

            return delegate(SlickFormatterContext ctx)
            {
                return Q.HtmlEncode(DateFormatter.Format(ctx.Value, format));
            };
        }

        public static SlickFormatter DateTime(string format = null)
        {
            format = format ?? Q.Culture.DateTimeFormat;

            return delegate(SlickFormatterContext ctx)
            {
                return Q.HtmlEncode(DateFormatter.Format(ctx.Value, format));
            };
        }

        public static SlickFormatter CheckBox()
        {
            return (ctx) => "<span class=\"check-box no-float " + (Q.IsTrue(ctx.Value) ? " checked" : "") + "\"></span>";
        }

        public static SlickFormatter Number(string format)
        {
            return delegate(SlickFormatterContext ctx)
            {
                return NumberFormatter.Format(ctx.Value, format);
            };
        }

        public static string GetItemType(jQueryObject link)
        {
            return link.GetDataValue("item-type") as string;
        }

        [Obsolete("Use GetItemType(link)")]
        public static string GetItemType(string href)
        {
            if (href.IsEmptyOrNull())
                return null;

            if (href.StartsWith("#"))
                href = href.Substr(1);

            var idx = href.LastIndexOf('/');
            if (idx >= 0)
                href = href.Substr(0, idx);

            return href;
        }

        public static string GetItemId(jQueryObject link)
        {
            var value = link.GetDataValue("item-id");
            return value == null ? null : value.ToString();
        }

        [Obsolete("Use GetItemId(link)")]
        public static string GetItemId(string href)
        {
            if (href.IsEmptyOrNull())
                return null;

            if (href.StartsWith("#"))
                href = href.Substr(1);

            var idx = href.LastIndexOf('/');
            if (idx >= 0)
                href = href.Substr(idx + 1);

            return href;
        }

        public static string ItemLinkText(string itemType, object id, object text, string extraClass)
        {
            return "<a" + (Script.IsValue(id) ? (" href=\"#" + itemType.Replace(".", "-") + "/" + id + "\"") : "") +
                " data-item-type=\"" + itemType + "\"" +
                " data-item-id=\"" + id + "\"" +
                " class=\"s-EditLink s-" + itemType.Replace(".", "-") + "Link" + (extraClass.IsEmptyOrNull() ? "" : (" " + extraClass)) + "\">" +
                Q.HtmlEncode(text ?? "") + "</a>";
        }

        public static SlickFormatter ItemLink(string itemType, string idField, 
            Func<SlickFormatterContext, string> getText, Func<SlickFormatterContext, string> cssClass = null)
        {
            return delegate(SlickFormatterContext ctx)
            {
                return ItemLinkText(itemType, ctx.Item[idField], 
                    getText == null ? ctx.Value : getText(ctx),
                    cssClass == null ? "" : cssClass(ctx));
            };
        }
    }
}