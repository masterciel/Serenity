﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Serenity.CodeGenerator.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public partial class EntityForm : RazorGenerator.Templating.RazorTemplateBase
    {
#line hidden

        #line 2 "..\..\Views\EntityForm.cshtml"
 public dynamic Model { get; set; } 
        #line default
        #line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");



            
            #line 2 "..\..\Views\EntityForm.cshtml"
                                                   
    var dotModule = Model.Module == null ? "" : ("." + Model.Module);
    var moduleDot = Model.Module == null ? "" : (Model.Module + ".");


            
            #line default
            #line hidden
WriteLiteral("namespace ");


            
            #line 6 "..\..\Views\EntityForm.cshtml"
      Write(Model.RootNamespace);

            
            #line default
            #line hidden

            
            #line 6 "..\..\Views\EntityForm.cshtml"
                            Write(dotModule);

            
            #line default
            #line hidden
WriteLiteral(".Forms\r\n{\r\n    using Serenity;\r\n    using Serenity.ComponentModel;\r\n    using Ser" +
"enity.Data;\r\n    using System;\r\n    using System.ComponentModel;\r\n    using Syst" +
"em.Collections.Generic;\r\n    using System.IO;\r\n\r\n    [FormScript(\"");


            
            #line 16 "..\..\Views\EntityForm.cshtml"
             Write(moduleDot);

            
            #line default
            #line hidden

            
            #line 16 "..\..\Views\EntityForm.cshtml"
                         Write(Model.ClassName);

            
            #line default
            #line hidden
WriteLiteral("\")]\r\n    [BasedOnRow(typeof(Entities.");


            
            #line 17 "..\..\Views\EntityForm.cshtml"
                            Write(Model.RowClassName);

            
            #line default
            #line hidden
WriteLiteral("))]\r\n    public class ");


            
            #line 18 "..\..\Views\EntityForm.cshtml"
             Write(Model.ClassName);

            
            #line default
            #line hidden

            
            #line 18 "..\..\Views\EntityForm.cshtml"
                                   WriteLiteral("Form\r\n    {");

            
            #line default
            #line hidden
            
            #line 19 "..\..\Views\EntityForm.cshtml"
      foreach (var x in Model.Fields)
    {
        if (x.Ident != Model.IdField)
        {
            
            #line default
            #line hidden
WriteLiteral("\r\n        public ");


            
            #line 23 "..\..\Views\EntityForm.cshtml"
          Write(x.Type);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 23 "..\..\Views\EntityForm.cshtml"
                  Write(x.Ident);

            
            #line default
            #line hidden
WriteLiteral(" { get; set; }");


            
            #line 23 "..\..\Views\EntityForm.cshtml"
                                                    }
    }

            
            #line default
            #line hidden
WriteLiteral("\r\n    }\r\n}");


        }
    }
}
#pragma warning restore 1591
