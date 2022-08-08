﻿import { Decorators } from "../../Decorators";
import { text, trimToNull } from "../../Q";
import { FilterDialog } from "./FilterDialog";
import { FilterWidgetBase } from "./FilterWidgetBase";

@Decorators.registerClass('Serenity.FilterDisplayBar')
export class FilterDisplayBar extends FilterWidgetBase<any> {

    constructor(div: JQuery) {
        super(div);

        this.element.find('.cap').text(
            text('Controls.FilterPanel.EffectiveFilter'));

        this.element.find('.edit').text(
            text('Controls.FilterPanel.EditFilter'));

        this.element.find('.reset').attr('title',
            text('Controls.FilterPanel.ResetFilterHint'));

        var openFilterDialog = (e: JQueryEventObject) => {
            e.preventDefault();
            var dialog = new FilterDialog();
            dialog.get_filterPanel().set_store(this.get_store());
            dialog.dialogOpen(null);
        };

        this.element.find('.edit').click(openFilterDialog);
        this.element.find('.txt').click(openFilterDialog);

        this.element.find('.reset').click(e1 => {
            e1.preventDefault();
            this.get_store().get_items().length = 0;
            this.get_store().raiseChanged();
        });
    }

    protected filterStoreChanged() {
        super.filterStoreChanged();

        var displayText = trimToNull(this.get_store().get_displayText());

        this.element.find('.current').toggle(displayText != null);
        this.element.find('.reset').toggle(displayText != null);

        if (displayText == null)
            displayText = text('Controls.FilterPanel.EffectiveEmpty');

        this.element.find('.txt').text('[' + displayText + ']');
    }

    protected getTemplate() {
        return "<div><a class='reset'></a><a class='edit'></a>" + 
            "<div class='current'><span class='cap'></span>" +
            "<a class='txt'></a></div></div>";
    }
}
