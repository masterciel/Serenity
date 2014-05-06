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
    public partial class EntityRepository : RazorGenerator.Templating.RazorTemplateBase
    {
#line hidden

        #line 2 "..\..\Views\EntityRepository.cshtml"
 public dynamic Model { get; set; } 
        #line default
        #line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");



            
            #line 2 "..\..\Views\EntityRepository.cshtml"
                                                   var dotModule = Model.Module == null ? "" : ("." + Model.Module);

            
            #line default
            #line hidden
WriteLiteral("\r\nnamespace ");


            
            #line 4 "..\..\Views\EntityRepository.cshtml"
      Write(Model.RootNamespace);

            
            #line default
            #line hidden

            
            #line 4 "..\..\Views\EntityRepository.cshtml"
                            Write(dotModule);

            
            #line default
            #line hidden
WriteLiteral(".Repositories\r\n{\r\n    using Serenity;\r\n    using Serenity.Data;\r\n    using Sereni" +
"ty.Services;\r\n    using System;\r\n    using System.Data;\r\n    using MyRow = Entit" +
"ies.");


            
            #line 11 "..\..\Views\EntityRepository.cshtml"
                       Write(Model.RowClassName);

            
            #line default
            #line hidden
WriteLiteral(";\r\n\r\n    public class ");


            
            #line 13 "..\..\Views\EntityRepository.cshtml"
             Write(Model.ClassName);

            
            #line default
            #line hidden
WriteLiteral("Repository\r\n    {\r\n        private static MyRow.RowFields fld { get { return MyRo" +
"w.Fields; } }\r\n\r\n        public SaveResponse Create(IUnitOfWork uow, SaveRequest" +
"<MyRow> request)\r\n        {\r\n            return new MySaveHandler().Process(uow," +
" request, SaveRequestType.Create);\r\n        }\r\n\r\n        public SaveResponse Upd" +
"ate(IUnitOfWork uow, SaveRequest<MyRow> request)\r\n        {\r\n            return " +
"new MySaveHandler().Process(uow, request, SaveRequestType.Update);\r\n        }\r\n\r" +
"\n        public DeleteResponse Delete(IUnitOfWork uow, DeleteRequest request)\r\n " +
"       {\r\n            return new MyDeleteHandler().Process(uow, request);\r\n     " +
"   }\r\n\r\n        public UndeleteResponse Undelete(IUnitOfWork uow, UndeleteReques" +
"t request)\r\n        {\r\n            return new MyUndeleteHandler().Process(uow, r" +
"equest);\r\n        }\r\n\r\n        public RetrieveResponse<MyRow> Retrieve(IDbConnec" +
"tion connection, RetrieveRequest request)\r\n        {\r\n            return new MyR" +
"etrieveHandler().Process(connection, request);\r\n        }\r\n\r\n        public List" +
"Response<MyRow> List(IDbConnection connection, ListRequest request)\r\n        {\r\n" +
"            return new MyListHandler().Process(connection, request);\r\n        }\r" +
"\n\r\n        private class MySaveHandler : SaveRequestHandler<MyRow> { }\r\n        " +
"private class MyDeleteHandler : DeleteRequestHandler<MyRow> { }\r\n        private" +
" class MyUndeleteHandler : UndeleteRequestHandler<MyRow> { }\r\n        private cl" +
"ass MyRetrieveHandler : RetrieveRequestHandler<MyRow> { }\r\n        private class" +
" MyListHandler : ListRequestHandler<MyRow> { }\r\n    }\r\n}");


        }
    }
}
#pragma warning restore 1591
