﻿using Newtonsoft.Json;
using Serenity.Data;
using System;
using System.Text;
using System.IO;
#if COREFX
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace Serenity.Services
{
    public class Result<TResponse> : ActionResult
    {
        public Encoding ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public JsonSerializerSettings SerializerSettings { get; set; }
        public TResponse Data { get; set; }
        public Formatting Formatting { get; set; }

        public Result(TResponse data)
        {
            this.Data = data;
            this.SerializerSettings = JsonSettings.Strict;
        }

#if COREFX
        public override void ExecuteResult(ActionContext context)
#else
        public override void ExecuteResult(ControllerContext context)
#endif
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var response = context.HttpContext.Response;
            response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : "application/json";

            if (ContentEncoding != null)
#if COREFX
                response.Headers["Content-Encoding"] = this.ContentEncoding.WebName;
#else
                response.ContentEncoding = this.ContentEncoding;
#endif
            if (Data != null)
            {
#if COREFX
                JsonTextWriter writer = new JsonTextWriter(new StreamWriter(response.Body)) { Formatting = this.Formatting };
#else
                JsonTextWriter writer = new JsonTextWriter(response.Output) { Formatting = this.Formatting };
#endif
                JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, Data);
                writer.Flush();
            }
        }
    }
}