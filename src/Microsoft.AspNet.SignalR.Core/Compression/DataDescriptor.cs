﻿using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.AspNet.SignalR.Compression
{
    public class DataDescriptor
    {
        public virtual string Name { get; set; }

        public virtual bool Enumerable { get; set; }

        public Type Type { get; set; }

        /// <summary>
        /// Sets the value taking in the baseObject and the newValue as the two object parameters
        /// </summary>
        public Action<object, object> SetValue { get; set; }

        /// <summary>
        /// Retrieves the value from the baseObject (parameter).
        /// </summary>
        public Func<object, object> GetValue { get; set; }
    }
}
