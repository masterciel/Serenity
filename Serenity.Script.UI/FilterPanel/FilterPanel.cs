﻿using jQueryApi;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Serenity
{
    [Imported(ObeysTypeSystem = true)]
    public partial class FilterPanel : FilterWidgetBase<object>
    {
        private jQueryObject rowsDiv;

        public FilterPanel(jQueryObject div)
            : base(div, null)
        {
            this.Element.AddClass("s-FilterPanel");
            rowsDiv = this.ById("Rows");

            InitButtons();
            UpdateButtons();
        }

        private bool showInitialLine;

        public bool ShowInitialLine
        {
            get { return showInitialLine; }
            set
            {
                if (showInitialLine != value)
                {
                    showInitialLine = value;
                    if (showInitialLine && rowsDiv.Children().Length == 0)
                        AddEmptyRow(false);
                }
            }
        }

        private bool showSearchButton = true;
        private bool updateStoreOnReset = false;

        protected override void FilterStoreChanged()
        {
            base.FilterStoreChanged();

            UpdateRowsFromStore();
        }

        protected void UpdateRowsFromStore()
        {
            rowsDiv.Empty();

            foreach (var item in Store.Items)
            {
                AddEmptyRow(false);
                var row = rowsDiv.Children().Last();

                var divl = row.Children("div.l");
                divl.Children(".leftparen").ToggleClass("active", Q.IsTrue(item.LeftParen));
                divl.Children(".rightparen").ToggleClass("active", Q.IsTrue(item.RightParen));
                divl.Children(".andor").ToggleClass("or", Q.IsTrue(item.IsOr))
                    .Text(Q.Text(Q.IsTrue(item.IsOr) ? "Controls.FilterPanel.Or" : "Controls.FilterPanel.And"));

                var fieldSelect = row.Children("div.f").Find("input.field-select").GetWidget<FilterFieldSelect>();
                fieldSelect.Value = item.Field;
                RowFieldChange(row);

                var operatorSelect = row.Children("div.o").Find("input.op-select").GetWidget<FilterOperatorSelect>();
                operatorSelect.Value = item.Operator;
                RowOperatorChange(row);

                var filtering = GetFilteringFor(row);
                if (filtering != null)
                {
                    filtering.Operator = item.Operator;
                    filtering.LoadState(item.State);
                }
            }

            if (ShowInitialLine && rowsDiv.Children().Length == 0)
                AddEmptyRow(false);

            UpdateParens();
        }

        public bool ShowSearchButton
        {
            get { return showSearchButton; }
            set
            {
                if (showSearchButton != value)
                {
                    showSearchButton = value;
                    UpdateButtons();
                }
            }
        }

        public bool UpdateStoreOnReset
        {
            get { return updateStoreOnReset; }
            set
            {
                if (updateStoreOnReset != value)
                {
                    updateStoreOnReset = value;
                }
            }
        }

        protected override string GetTemplate()
        {
            return PanelTemplate;
        }

        private void InitButtons()
        {
            this.ById("AddButton").Text(Q.Text("Controls.FilterPanel.AddFilter")).Click(AddButtonClick);
            this.ById("SearchButton").Text(Q.Text("Controls.FilterPanel.SearchButton")).Click(SearchButtonClick);
            this.ById("ResetButton").Text(Q.Text("Controls.FilterPanel.ResetButton")).Click(ResetButtonClick);
        }

        private void SearchButtonClick(jQueryEvent e)
        {
            e.PreventDefault();
            Search();
        }

        public bool HasErrors
        {
            get
            {
                return this.rowsDiv.Children().Children("div.v").Children("span.error").Length > 0;
            }
        }

        public void Search()
        {
            this.rowsDiv.Children().Children("div.v").Children("span.error").Remove();

            var filterLines = new List<FilterLine>();
            string errorText = null;
            jQueryObject row = null;

            for (int i = 0; i < rowsDiv.Children().Length; i++)
            {
                try
                {
                    row = rowsDiv.Children().Eq(i);

                    var filtering = GetFilteringFor(row);
                    if (filtering == null)
                        continue;

                    var field = GetFieldFor(row);
                    var op = row.Children("div.o").Find("input.op-select").GetWidget<FilterOperatorSelect>().Value;

                    if (op == null || op.Length == 0)
                        throw new ArgumentOutOfRangeException("operator", Q.Text("Controls.FilterPanel.InvalidOperator"));

                    FilterLine line = new FilterLine();
                    line.Field = field.Name;
                    line.Operator = op;
                    line.IsOr = row.Children("div.l").Children("a.andor").HasClass("or");
                    line.LeftParen = row.Children("div.l").Children("a.leftparen").HasClass("active");
                    line.RightParen = row.Children("div.l").Children("a.rightparen").HasClass("active");
                    filtering.Operator = op;
                    var criteria = filtering.GetCriteria();
                    line.Criteria = criteria.Criteria;
                    line.State = filtering.SaveState();
                    line.DisplayText = criteria.DisplayText;

                    filterLines.Add(line);
                }
                catch (ArgumentException ex)
                {
                    errorText = ex.Message;
                    break;
                }
            }

            // if an error occured, display it, otherwise set current filters
            if (errorText != null)
            {
                J("<span/>").AddClass("error").Attribute("title", errorText).AppendTo(row.Children("div.v"));
                row.Children("div.v").Find("input:first").Focus();
                return;
            }

            Store.Items.Clear();
            Store.Items.AddRange(filterLines);
            Store.RaiseChanged();
        }

        private void AddButtonClick(jQueryEvent e)
        {
            AddEmptyRow(true);
            e.PreventDefault();
        }

        private void ResetButtonClick(jQueryEvent e)
        {
            e.PreventDefault();

            if (UpdateStoreOnReset)
            {
                if (Store.Items.Count > 0)
                {
                    Store.Items.Clear();
                    Store.RaiseChanged();
                }
            }

            rowsDiv.Empty();
            UpdateButtons();

            if (ShowInitialLine)
                AddEmptyRow(false);
        }

        private jQueryObject FindEmptyRow()
        {
            jQueryObject result = null;

            rowsDiv.Children().Each((index, row) =>
            {
                jQueryObject fieldInput = J(row).Children("div.f").Children("input.field-select").First();
                if (fieldInput.Length == 0)
                    return true;

                string val = fieldInput.GetValue();
                if (val == null || val.Length == 0)
                {
                    result = J(row);
                    return false;
                }

                return true;
            });

            return result;
        }

        private jQueryObject AddEmptyRow(bool popupField)
        {
            jQueryObject emptyRow = FindEmptyRow();

            if (emptyRow != null)
            {
                emptyRow.Find("input.field-select").Select2("focus");

                if (popupField)
                    emptyRow.Find("input.field-select").Select2("open");

                return emptyRow;
            }

            bool isLastRowOr = this.rowsDiv.Children().Last().Children("a.andor").HasClass("or");

            jQueryObject row = J(RowTemplate).AppendTo(this.rowsDiv);

            jQueryObject parenDiv = row.Children("div.l").Hide();

            parenDiv.Children("a.leftparen, a.rightparen").Click(LeftRightParenClick);

            jQueryObject andor = parenDiv.Children("a.andor").Attribute("title", Q.Text("Controls.FilterPanel.ChangeAndOr"));

            if (isLastRowOr)
                andor.AddClass("or").Text(Q.Text("Controls.FilterPanel.Or"));
            else
                andor.Text(Q.Text("Controls.FilterPanel.And"));

            andor.Click(AndOrClick);

            row.Children("a.delete").Attribute("title", Q.Text("Controls.FilterPanel.RemoveField")).Click(DeleteRowClick);

            var fieldSel = new FilterFieldSelect(row.Children("div.f").Children("input"), this.Store.Fields);
            fieldSel.ChangeSelect2(OnRowFieldChange);

            UpdateParens();
            UpdateButtons();

            row.Find("input.field-select").Select2("focus");

            if (popupField)
                row.Find("input.field-select").Select2("open");

            return row;
        }

        private void OnRowFieldChange(jQueryEvent e)
        {
            jQueryObject row = J(e.Target).Closest("div.filter-line");
            RowFieldChange(row);
            jQueryObject opSelect = row.Children("div.o").Find("input.op-select");
            opSelect.Select2("focus");
        }

        private void RowFieldChange(jQueryObject row)
        {
            row.RemoveData("Filtering");

            var select = row.Children("div.f").Find("input.field-select").GetWidget<FilterFieldSelect>();
            string fieldName = select.Value;

            bool isEmpty = (fieldName == null || fieldName == "");

            RemoveFiltering(row);
            PopulateOperatorList(row);
            RowOperatorChange(row);
            UpdateParens();
            UpdateButtons();
        }

        private void RemoveFiltering(jQueryObject row)
        {
            row.Data("Filtering", null);
            row.Data("FilteringField", null);
        }

        private void PopulateOperatorList(jQueryObject row)
        {
            row.Children("div.o").Html("");

            IFiltering filtering = GetFilteringFor(row);
            if (filtering == null)
                return;
            
            var hidden = row.Children("div.o").Html("<input/>").Children().Attribute("type", "hidden").AddClass("op-select");

            var operators = filtering.GetOperators();

            new FilterOperatorSelect(hidden, operators).ChangeSelect2(OnRowOperatorChange);
        }

        private PropertyItem GetFieldFor(jQueryObject row)
        {
            if (row.Length == 0)
                return null;

            var select = row.Children("div.f").Find("input.field-select").GetWidget<FilterFieldSelect>();
            if (select.Value.IsEmptyOrNull())
                return null;

            return this.Store.FieldByName[select.Value];
        }

        private IFiltering GetFilteringFor(jQueryObject row)
        {
            var field = GetFieldFor(row);
            if (field == null)
                return null;

            IFiltering filtering = (IFiltering)row.GetDataValue("Filtering");

            if (filtering != null)
                return filtering;

            var filteringType = FilteringTypeRegistry.Get(field.FilteringType ?? "String");

            jQueryObject editorDiv = row.Children("div.v");

            filtering = (IFiltering)Activator.CreateInstance(filteringType);
            ReflectionOptionsSetter.Set(filtering, field.FilteringParams);
            filtering.Container = editorDiv;
            filtering.Field = field;
            row.Data("Filtering", filtering);
            
            return filtering;
        }

        private void OnRowOperatorChange(jQueryEvent e)
        {
            jQueryObject row = J(e.Target).Closest("div.filter-line");
            RowOperatorChange(row);
            jQueryObject firstInput = row.Children("div.v").Find(":input:visible").First();
            try { firstInput.Focus(); }
            catch { };
        }

        private void RowOperatorChange(jQueryObject row)
        {
            if (row.Length == 0)
                return;

            jQueryObject editorDiv = row.Children("div.v");

            editorDiv.Html("");

            IFiltering filtering = GetFilteringFor(row);

            if (filtering == null)
                return;

            var operatorSelect = row.Children("div.o").Find("input.op-select").GetWidget<FilterOperatorSelect>();
            if (operatorSelect.Value.IsEmptyOrNull())
                return;

            var ops = filtering.GetOperators().Filter(x => x.Key == operatorSelect.Value);
            var op = ops.Count > 0 ? ops[0] : null;
            if (op == null)
                return;

            filtering.Operator = op;
            filtering.CreateEditor();
        }

        private void DeleteRowClick(jQueryEvent e)
        {
            e.PreventDefault();
            jQueryObject row = J(e.Target).Closest("div.filter-line");
            row.Remove();
            if (this.rowsDiv.Children().Length == 0)
                Search();

            UpdateParens();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            this.ById("SearchButton").Toggle(rowsDiv.Children().Length >= 1 && showSearchButton);
            this.ById("ResetButton").Toggle(rowsDiv.Children().Length >= 1);
        }

        private void AndOrClick(jQueryEvent e)
        {
            e.PreventDefault();
            jQueryObject andor = J(e.Target).ToggleClass("or");
            andor.Text(Q.Text("Controls.FilterPanel." + (andor.HasClass("or") ? "Or" : "And")));
        }

        private void LeftRightParenClick(jQueryEvent e)
        {
            e.PreventDefault();
            J(e.Target).ToggleClass("active");
            UpdateParens();
        }

        private void UpdateParens()
        {
            jQueryObject rows = rowsDiv.Children();
            if (rows.Length == 0)
                return;

            rows.RemoveClass("paren-start");
            rows.RemoveClass("paren-end");

            rows.Children("div.l").CSS("display", rows.Length == 1 ? "none" : "block");

            rows.First().Children("div.l").Children("a.rightparen, a.andor").CSS("visibility", "hidden");

            for (int i = 1; i < rows.Length; i++)
            {
                jQueryObject row = rows.Eq(i);
                row.Children("div.l").CSS("display", "block")
                    .Children("a.lefparen, a.andor").CSS("visibility", "visible");
            }

            bool inParen = false;
            for (int i = 0; i < rows.Length; i++)
            {
                jQueryObject row = rows.Eq(i);
                jQueryObject divParen = row.Children("div.l");
                jQueryObject lp = divParen.Children("a.leftparen");
                jQueryObject rp = divParen.Children("a.rightparen");

                if (rp.HasClass("active") && inParen)
                {
                    inParen = false;
                    if (i > 0)
                        rows.Eq(i - 1).AddClass("paren-end");
                }

                if (lp.HasClass("active"))
                {
                    inParen = true;
                    if (i > 0)
                        row.AddClass("paren-start");
                }
            }
        }

        public const string PanelTemplate =
            "<div id='~_Rows' class='filter-lines'>" +
            "</div>" +
            "<div id='~_Buttons' class='buttons'>" +
                "<button id='~_AddButton' class='btn btn-primary add'></button>" +
                "<button id='~_SearchButton' class='btn btn-success search'></button>" +
                "<button id='~_ResetButton' class='btn btn-danger reset'></button>" +
            "</div>" +
            "<div style='clear: both'>" +
            "</div>";

        public const string RowTemplate =
            "<div class='filter-line'>" +
                "<a class='delete'><span></span></a>" +
                "<div class='l'>" +
                    "<a class='rightparen' href='#'>)</a>" +
                    "<a class='andor' href='#'></a>" +
                    "<a class='leftparen' href='#'>(</a>" +
                "</div>" +
                "<div class='f'>" +
                    "<input type='hidden' class='field-select'>" +
                "</div>" +
                "<div class='o'></div>" +
                "<div class='v'></div>" +
                "<div style='clear: both'></div>" +
            "</div>";
    }
}