﻿
namespace Serenity {

    export namespace Decorators {
        function distinct(arr: any[]) {
            return arr.filter((item, pos) => arr.indexOf(item) === pos);
        }

        function merge(arr1: any[], arr2: any[]) {
            if (!arr1 || !arr2)
                return (arr1 || arr2 || []).slice();

            return distinct(arr1.concat(arr2));
        }

        function registerType(target: any, name: string, intf: any[]) {
            if (name != null) {
                target.__typeName = name;
                target.__assembly = ss.__assemblies['App'];
                target.__assembly.__types[name] = target;
            }
            else if (!target.__typeName)
                target.__register = true;

            if (intf)
                target.__interfaces = merge(target.__interfaces, intf);
        }

        export function registerClass(nameOrIntf?: string | any[], intf2?: any[]) {
            return function (target: Function) {
                if (typeof nameOrIntf == "string")
                    registerType(target, nameOrIntf, intf2);
                else
                    registerType(target, null, nameOrIntf);

                (target as any).__class = true;
            }
        }

        export function registerInterface(nameOrIntf?: string | any[], intf2?: any[]) {
            return function (target: Function) {

                if (typeof nameOrIntf == "string")
                    registerType(target, nameOrIntf, intf2);
                else
                    registerType(target, null, nameOrIntf);

                (target as any).__interface = true;
                (target as any).isAssignableFrom = function (type: any) {
                    return (ss as any).contains((ss as any).getInterfaces(type), this);
                };
            }
        }

        export function registerEditor(nameOrIntf?: string | any[], intf2?: any[]) {
            return registerClass(nameOrIntf, intf2);
        }
    }
}

namespace System.ComponentModel {

    @Serenity.Decorators.registerClass('System.DisplayNameAttribute')
    export class DisplayNameAttribute {
        constructor(public displayName: string) {
        }
    }
}

namespace Serenity {

    @Decorators.registerInterface('Serenity.ISlickFormatter')
    export class ISlickFormatter {
    }

    function Attr(name: string) {
        return Decorators.registerClass('Serenity.' + name + 'Attribute')
    }

    @Attr('Category')
    export class CategoryAttribute {
        constructor(public category: string) {
        }
    }

    @Attr('Collapsible')
    export class CollapsibleAttribute {
        constructor(public value: boolean) {
        }

        public collapsed: boolean;
    }

    @Attr('ColumnsKey')
    export class ColumnsKeyAttribute {
        constructor(public value: string) {
        }
    }


    @Attr('CssClass')
    export class CssClassAttribute {
        constructor(public cssClass: string) {
        }
    }

    @Attr('DefaultValue')
    export class DefaultValueAttribute {
        constructor(public value: any) {
        }
    }

    @Attr('DialogType')
    export class DialogTypeAttribute {
        constructor(public value: Function) {
        }
    }

    @Attr('Editor')
    export class EditorAttribute {
        constructor() { }
        key: string;
    }

    @Attr('EditorOption')
    export class EditorOptionAttribute {
        constructor(public key: string, public value: any) {
        }
    }

    @Decorators.registerClass('Serenity.EditorTypeAttributeBase')
    export class EditorTypeAttributeBase {
        constructor(public editorType: string) {
        }

        public setParams(editorParams: any): void {
        }
    }

    @Attr('EditorType')
    export class EditorTypeAttribute extends EditorTypeAttributeBase {
        constructor(editorType: string) {
            super(editorType);
        }
    }

    @Attr('Element')
    export class ElementAttribute {
        constructor(public value: string) {
        }
    }

    @Attr('EntityType')
    export class EntityTypeAttribute {
        constructor(public value: string) {
        }
    }

    @Attr('EnumKey')
    export class EnumKeyAttribute {
        constructor(public value: string) {
        }
    }

    @Attr('Flexify')
    export class FlexifyAttribute {
        constructor(public value = true) {
        }
    }

    @Attr('Filterable')
    export class FilterableAttribute {
        constructor(public value = true) {
        }
    }

    @Attr('FormKey')
    export class FormKeyAttribute {
        constructor(public value: string) {
        }
    }

    @Attr('GeneratedCode')
    export class GeneratedCodeAttribute {
        constructor(public origin?: string) {
        }
    }

    @Attr('IdProperty')
    export class IdPropertyAttribute {
        constructor(public value: string) {
        }
    }

    @Attr('IsActiveProperty')
    export class IsActivePropertyAttribute {
        constructor(public value: string) {
        }
    }

    @Attr('ItemName')
    export class ItemNameAttribute {
        constructor(public value: string) {
        }
    }

    @Attr('LocalTextPrefix')
    export class LocalTextPrefixAttribute {
        constructor(public value: string) {
        }
    }

    @Attr('Maximizable')
    export class MaximizableAttribute {
        constructor(public value = true) {
        }
    }

    @Attr('NameProperty')
    export class NamePropertyAttribute {
        constructor(public value: string) {
        }
    }

    @Attr('Option')
    export class OptionAttribute {
        constructor() {
        }
    }

    @Attr('OptionsType')
    export class OptionsTypeAttribute {
        constructor(public value: Function) {
        }
    }

    @Attr('Panel')
    export class PanelAttribute {
        constructor(public value = true) {
        }
    }

    @Attr('Resizable')
    export class ResizableAttribute {
        constructor(public value = true) {
        }
    }

    @Attr('Responsive')
    export class ResponsiveAttribute {
        constructor(public value = true) {
        }
    }

    @Attr('Service')
    export class ServiceAttribute {
        constructor(public value: string) {
        }
    }
}

namespace Serenity.Decorators {

    export function registerFormatter(nameOrIntf: string | any[] = [ISlickFormatter], intf2: any[] = [ISlickFormatter]) {
        return registerClass(nameOrIntf, intf2);
    }

    export function addAttribute(type: any, attr: any) {
        type.__metadata = type.__metadata || {};
        type.__metadata.attr = type.__metadata.attr || [];
        type.__metadata.attr.push(attr);
    }

    export function addMemberAttr(type: any, memberName: string, attr: any) {
        type.__metadata = type.__metadata || {};
        type.__metadata.members = type.__metadata.members || [];
        let member: any = undefined;
        for (var m of type.__metadata.members) {
            if (m.name == memberName) {
                member = m;
                break;
            }
        }

        if (!member) {
            member = { name: memberName, attr: [], type: 4, returnType: Object, sname: memberName };
            type.__metadata.members.push(member);
        }

        member.attr = member.attr || [];
        member.attr.push(attr);
    }

    export function columnsKey(value: string) {
        return function (target: Function) {
            addAttribute(target, new ColumnsKeyAttribute(value));
        }
    }

    export function dialogType(value: Function) {
        return function (target: Function) {
            addAttribute(target, new DialogTypeAttribute(value));
        }
    }

    export function editor(key?: string) {
        return function (target: Function) {
            var attr = new EditorAttribute();
            if (key !== undefined)
                attr.key = key;
            addAttribute(target, attr);
        }
    }

    export function element(value: string) {
        return function (target: Function) {
            addAttribute(target, new ElementAttribute(value));
        }
    }

    export function entityType(value: string) {
        return function (target: Function) {
            addAttribute(target, new EntityTypeAttribute(value));
        }
    }

    export function enumKey(value: string) {
        return function (target: Function) {
            addAttribute(target, new EnumKeyAttribute(value));
        }
    }

    export function flexify(value = true) {
        return function (target: Function) {
            addAttribute(target, new FlexifyAttribute(value));
        }
    }

    export function formKey(value: string) {
        return function (target: Function) {
            addAttribute(target, new FormKeyAttribute(value));
        }
    }

    export function generatedCode(origin?: string) {
        return function (target: Function) {
            addAttribute(target, new GeneratedCodeAttribute(origin));
        }
    }

    export function idProperty(value: string) {
        return function (target: Function) {
            addAttribute(target, new IdPropertyAttribute(value));
        }
    }

    export function registerEnum(target: any, enumKey?: string, name?: string) {
        if (!target.__enum) {
            Object.defineProperty(target, '__enum', {
                get: function () {
                    return true;
                }
            });

            target.prototype = target.prototype || {};
            for (var k of Object.keys(target))
                if (isNaN(Q.parseInteger(k)) && target[k] != null && !isNaN(Q.parseInteger(target[k])))
                    target.prototype[k] = target[k];

            if (name != null) {
                target.__typeName = name;
                target.__assembly = ss.__assemblies['App'];
                target.__assembly.__types[name] = target;
            }
            else if (!target.__typeName)
                target.__register = true;

            if (enumKey)
                addAttribute(target, new EnumKeyAttribute(enumKey));
        }
    }

    export function registerEnumType(target: any, name?: string, enumKey?: string) {
        registerEnum(target, name, Q.coalesce(enumKey, name));
    }

    export function filterable(value = true) {
        return function (target: Function) {
            addAttribute(target, new FilterableAttribute(value));
        }
    }

    export function itemName(value: string) {
        return function (target: Function) {
            addAttribute(target, new ItemNameAttribute(value));
        }
    }

    export function isActiveProperty(value: string) {
        return function (target: Function) {
            addAttribute(target, new IsActivePropertyAttribute(value));
        }
    }

    export function localTextPrefix(value: string) {
        return function (target: Function) {
            addAttribute(target, new LocalTextPrefixAttribute(value));
        }
    }

    export function maximizable(value = true) {
        return function (target: Function) {
            addAttribute(target, new MaximizableAttribute(value));
        }
    }

    export function nameProperty(value: string) {
        return function (target: Function) {
            addAttribute(target, new NamePropertyAttribute(value));
        }
    }

    export function option() {
        return function (target: Object, propertyKey: string): void {
            addMemberAttr(target.constructor, propertyKey, new OptionAttribute());
        }
    }

    export function optionsType(value: Function) {
        return function (target: Function) {
            addAttribute(target, new OptionsTypeAttribute(value));
        }
    }

    export function panel(value = true) {
        return function (target: Function) {
            addAttribute(target, new PanelAttribute(value));
        }
    }

    export function resizable(value = true) {
        return function (target: Function) {
            addAttribute(target, new ResizableAttribute(value));
        }
    }

    export function responsive(value = true) {
        return function (target: Function) {
            addAttribute(target, new ResponsiveAttribute(value));
        }
    }

    export function service(value: string) {
        return function (target: Function) {
            addAttribute(target, new ServiceAttribute(value));
        }
    }
}

declare namespace Serenity {

    class HiddenAttribute {
    }

    class HintAttribute {
        constructor(hint: string);
        hint: string;
    }

    class InsertableAttribute {
        constructor(insertable?: boolean);
        value: boolean;
    }

    class MaxLengthAttribute {
        constructor(maxLength: number);
        maxLength: number;
    }

    class OneWayAttribute {
    }

    class PlaceholderAttribute {
        constructor(value: string);
        value: string;
    }

    class ReadOnlyAttribute {
        constructor(readOnly?: boolean);
        value: boolean;
    }

    class RequiredAttribute {
        constructor(isRequired: boolean);
        isRequired: boolean;
    }

    class UpdatableAttribute {
        constructor(updatable?: boolean);
        value: boolean;
    }
}