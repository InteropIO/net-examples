﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Glue.Logging;

namespace GlueBlazor.Data
{
    internal class GlueLoggerFactory : IGlueLoggerFactory
    {
        private readonly ILoggerFactory loggerFactory_;

        public GlueLoggerFactory(ILoggerFactory loggerFactory)
        {
            loggerFactory_ = loggerFactory;
        }

        public IGlueLog GetLogger(string name)
        {
            return new GlueLogger(loggerFactory_.CreateLogger(name));
        }

        public IGlueLog GetLogger(Type type)
        {
            return new GlueLogger(loggerFactory_.CreateLogger(type));
        }
    }
}
