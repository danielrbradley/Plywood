using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Plywood
{
    public abstract class ControllerBase
    {
        public const string STR_INFO_FILE_NAME = ".info";

        private ControllerConfiguration _Context;
        public ControllerConfiguration Context { get { return _Context; } }

        public ControllerBase()
        {
            // Load context from configuration.
            _Context = Configuration.AppSettings.LoadControllerConfiguration();
        }

        public ControllerBase(ControllerConfiguration context)
        {
            _Context = context;
        }
    }
}