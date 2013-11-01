﻿using jQueryApi;
using jQueryApi.UI.Widgets;
using System.Html;
using System.Runtime.CompilerServices;

namespace Serenity
{
    public interface IDialog
    {
    }

    public abstract class TemplatedDialog<TOptions> : TemplatedWidget<TOptions>, IDialog
        where TOptions : class, new()
    {
        protected jQueryValidator validator;

        protected TemplatedDialog(jQueryObject element, TOptions options)
            : base(element ?? Q.NewBodyDiv(), options)
        {
            InitDialog();
            InitValidator();
        }

        public override void Destroy()
        {
            element.Dialog().Destroy();
            base.Destroy();
        }

        protected virtual void InitDialog()
        {
            element.Dialog(GetDialogOptions());

            var self = this;
            element.Bind("dialogopen." + this.uniqueName, delegate
            {
                this.DialogOpen();
            });

            element.Bind("dialogclose." + this.uniqueName, delegate
            {
                this.DialogClose();
            });
        }

        protected virtual jQueryValidatorOptions GetValidatorOptions()
        {
            return new jQueryValidatorOptions();
        }

        protected virtual void InitValidator()
        {
            var form = this.ById("Form");
            if (form.Length > 0)
            {
                var valOptions = GetValidatorOptions();
                validator = form.As<jQueryValidationObject>().Validate(Q.Externals.ValidateOptions(valOptions));
            }
        }

        protected virtual void ResetValidation()
        {
            if (validator != null)
                ((dynamic)validator).resetAll();
        }

        protected virtual bool ValidateForm()
        {
            return validator == null || Q.IsTrue(validator.ValidateForm());
        }

        public void DialogOpen()
        {
            element.Dialog().Open();
        }

        protected virtual void OnDialogOpen()
        {
            jQuery.Select(":input:eq(0)", element).Focus();
            this.Arrange();
        }

        protected virtual void Arrange()
        {
        }

        [InlineCode("$.qtip")]
        private object GetQTipPlugin()
        {
            return false;
        }

        protected virtual void OnDialogClose()
        {
            jQuery.Document.Trigger("click"); // for tooltips etc.

            if (GetQTipPlugin() != null)
            {
                jQuery.FromElement(Document.Body).Children(".qtip").Each(delegate(int index, Element el)
                {
                    ((dynamic)jQuery.FromElement(el)).qtip("hide");
                });
            }

            var self = this;
            Window.SetTimeout(delegate()
            {
                var element = self.element;
                self.Destroy();
                element.Remove();
            }, 0);
        }

        protected virtual DialogOptions GetDialogOptions()
        {
            DialogOptions opt = new DialogOptions();

            opt.DialogClass = "s-Dialog " + "s-" + this.GetType().Name;
            opt.Width = 920;
            opt.AutoOpen = false;
            opt.Resizable = false;
            opt.Modal = true;
            opt.Position = new string[] { "center", "center" };

            return opt;
        }

        public void DialogClose()
        {
            this.element.Dialog().Close();
        }

        public string IdPrefix { get { return idPrefix; } }
    }
}