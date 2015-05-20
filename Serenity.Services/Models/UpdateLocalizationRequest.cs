﻿using Serenity.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Serenity.Services
{
    public class UpdateLocalizationRequest<TEntity> : ServiceRequest
    {
        public Dictionary<string, TEntity> Entities { get; set; }
    }
}