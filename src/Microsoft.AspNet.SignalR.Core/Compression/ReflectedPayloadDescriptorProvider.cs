﻿using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Compression
{
    public class ReflectedPayloadDescriptorProvider : IPayloadDescriptorProvider
    {
        private readonly Lazy<IDictionary<Type, PayloadDescriptor>> _payloads;
        private readonly Lazy<IAssemblyLocator> _locator;

        private static long _payloadDescriptorID = 0;

        public ReflectedPayloadDescriptorProvider(IDependencyResolver resolver)
        {
            _locator = new Lazy<IAssemblyLocator>(resolver.Resolve<IAssemblyLocator>);
            _payloads = new Lazy<IDictionary<Type, PayloadDescriptor>>(BuildPayloadsCache);
        }

        protected IDictionary<Type, PayloadDescriptor> BuildPayloadsCache()
        {
            // Getting all payloads that have a payload attribute
            var types = _locator.Value.GetAssemblies()
                        .SelectMany(GetTypesSafe)
                        .Where(HasPayloadAttribute);
            
            // Building cache entries for each descriptor
            // Each descriptor is stored in dictionary under a key
            // that is it's name
            var cacheEntries = types
                .Select(type => new PayloadDescriptor
                {
                    Type = type,
                    ID = Interlocked.Increment(ref _payloadDescriptorID),
                    Data = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Select(propertyInfo => new DataDescriptor
                                {
                                    Name = propertyInfo.Name,
                                    Type = propertyInfo.PropertyType,
                                    SetValue = (baseObject, newValue) =>
                                    {
                                        propertyInfo.SetValue(baseObject, newValue, null);
                                    },
                                    GetValue = (baseObject) =>
                                    {
                                        return propertyInfo.GetValue(baseObject, null);
                                    }
                                })
                               .Union(type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                               .Select(fieldInfo => new DataDescriptor
                               {
                                    Name = fieldInfo.Name,
                                    Type = fieldInfo.FieldType,
                                    SetValue = (baseObject, newValue) =>
                                    {
                                        fieldInfo.SetValue(baseObject, newValue);
                                    },
                                    GetValue = (baseObject) =>
                                    {
                                        return fieldInfo.GetValue(baseObject);
                                    }
                               }))
                               .OrderBy(dataDescriptor => dataDescriptor.Name)
                })
                .ToDictionary(payload => payload.Type,
                              payload => payload);

            return cacheEntries;
        }

        public IEnumerable<PayloadDescriptor> GetPayloads()
        {
            return _payloads.Value
                .Select(a => a.Value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "If we throw then we have an empty type")]
        private static IEnumerable<Type> GetTypesSafe(Assembly a)
        {
            try
            {
                return a.GetTypes();
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }

        private static bool HasPayloadAttribute(Type type)
        {
            try
            {
                return Attribute.IsDefined(type, typeof(PayloadAttribute));
            }
            catch
            {
                return false;
            }
        }


        public PayloadDescriptor GetPayload(Type type)
        {
            if (IsPayload(type))
            {
                return _payloads.Value[type];
            }
            
            return null;
        }


        public bool IsPayload(Type type)
        {
            return _payloads.Value.Keys.Contains(type);
        }
    }
}
