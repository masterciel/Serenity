﻿using jQueryApi;
using jQueryApi.UI.Widgets;
using Serenity;
using System;
using System.Collections.Generic;

namespace Serenity
{
    public abstract partial class EntityDialog<TEntity, TOptions> : TemplatedDialog<TOptions>
        where TEntity : class, new()
        where TOptions: class, new()
    {
        protected jQueryObject saveAndCloseButton;
        protected jQueryObject applyChangesButton;
        protected jQueryObject deleteButton;
        protected jQueryObject undeleteButton;
        protected jQueryObject cloneButton;

        protected override void InitToolbar()
        {
            base.InitToolbar();

            if (this.toolbar == null)
                return;

            saveAndCloseButton = toolbar.FindButton("save-and-close-button");
            applyChangesButton = toolbar.FindButton("apply-changes-button");
            deleteButton = toolbar.FindButton("delete-button");
            undeleteButton = toolbar.FindButton("undo-delete-button");
            cloneButton = toolbar.FindButton("clone-button");
        }

        protected virtual void ShowSaveSuccessMessage(ServiceResponse response)
        {
            Q.NotifySuccess("Kayıt işlemi başarılı");
        }

        protected override List<ToolButton> GetToolbarButtons()
        {
            List<ToolButton> list = new List<ToolButton>();

            var self = this;

            list.Add(new ToolButton 
            {
                Title = "Kaydet",
                CssClass = "save-and-close-button",
                OnClick = delegate
                {
                    self.Save(delegate(ServiceResponse response)
                    {
                        self.element.Dialog().Close();
                    });
                }
            });

            list.Add(new ToolButton
            {
                Title = "",
                Hint = "Değişiklikleri Uygula",
                CssClass = "apply-changes-button",
                OnClick = delegate
                {
                    if (self.IsLocalizationMode)
                    {
                        self.SaveLocalization();
                        return;
                    }

                    self.Save(delegate(ServiceResponse response)
                    {
                        if (self.IsEditMode)
                            self.LoadById(self.EntityId.As<long>(), null);
                        else
                            self.LoadById(((object)(response.As<dynamic>().EntityId)).As<long>(), null);

                        ShowSaveSuccessMessage(response);
                    });
                }
            });

            list.Add(new ToolButton
            {
                Title = "Sil",
                CssClass = "delete-button",
                OnClick = delegate
                {
                    Q.Confirm("Kaydı silmek istiyor musunuz?", delegate
                    {
                        self.DoDelete(delegate
                        {
                            self.element.Dialog().Close();
                        });
                    });
                }
            });

            list.Add(new ToolButton
            {
                Title = "Geri Al",
                CssClass = "undo-delete-button",
                OnClick = delegate
                {
                    if (self.IsDeleted)
                    {
                        Q.Confirm("Kaydı geri almak istiyor musunuz?", delegate()
                        {
                            self.Undelete(delegate
                            {
                                self.LoadById(self.EntityId.As<long>(), null);
                            });
                        });
                    }
                }
            });

            list.Add(new ToolButton
            {
                Title = "Klonla",
                CssClass = "clone-button",
                OnClick = delegate
                {
                    if (!self.IsEditMode)
                        return;

                    var cloneEntity = GetCloningEntity();
                    var cloneDialog = Activator.CreateInstance(this.GetType(), new object()).As<EntityDialog<TEntity, TOptions>>();
                    cloneDialog.Cascade(this.element).BubbleDataChange(this).LoadEntityAndOpenDialog(cloneEntity);
                }
            });
            
            return list;
        }

        protected virtual TEntity GetCloningEntity()
        {
            var clone = new TEntity();
            clone = jQuery.Extend(clone, this.Entity).As<TEntity>().As<TEntity>();

            var idField = GetEntityIdField();
            if (!idField.IsEmptyOrNull())
                Script.Delete(clone, idField);

            var isActiveField = GetEntityIsActiveField();
            if (!isActiveField.IsEmptyOrNull())
                Script.Delete(clone, isActiveField);

            return clone;
        }

        protected virtual void UpdateInterface()
        {
            bool isDeleted = IsDeleted;
            bool isLocalizationMode = IsLocalizationMode;

            if (deleteButton != null)
                deleteButton.Toggle(!isLocalizationMode && IsEditMode && !isDeleted);

            if (undeleteButton != null)
                undeleteButton.Toggle(!isLocalizationMode && IsEditMode && isDeleted);

            if (saveAndCloseButton != null)
            {
                saveAndCloseButton.Toggle(!isLocalizationMode && !isDeleted);

                saveAndCloseButton.Find(".button-inner").Text(
                    IsNew ? "Kaydet" : "Güncelle");
            }

            if (applyChangesButton != null)
                applyChangesButton.Toggle(isLocalizationMode || !isDeleted);

            if (cloneButton != null)
                cloneButton.Toggle(false);

            if (propertyGrid != null)
                propertyGrid.Element.Toggle(!isLocalizationMode);

            if (localizationGrid != null)
                localizationGrid.Element.Toggle(isLocalizationMode);

            if (localizationSelect != null)
                localizationSelect.Toggle(IsEditMode && !IsCloneMode);

            if (tabs != null)
                tabs.SetDisabled("Log", IsNewOrDeleted);
        }
    }
}