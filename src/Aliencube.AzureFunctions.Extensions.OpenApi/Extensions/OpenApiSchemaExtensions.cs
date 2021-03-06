﻿using System;
using System.Collections;
using System.Linq;
using System.Reflection;

using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aliencube.AzureFunctions.Extensions.OpenApi.Extensions
{
    /// <summary>
    /// This represents the extension entity for <see cref="OpenApiSchema"/>.
    /// </summary>
    public static class OpenApiSchemaExtensions
    {
        /// <summary>
        /// Converts <see cref="Type"/> to <see cref="OpenApiSchema"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> instance.</param>
        /// <param name="attribute"><see cref="OpenApiSchemaVisibilityAttribute"/> instance. Default is <c>null</c>.</param>
        /// <returns><see cref="OpenApiSchema"/> instance.</returns>
        /// <remarks>
        /// It runs recursively to build the entire object type. It only takes properties without <see cref="JsonIgnoreAttribute"/>.
        /// </remarks>
        public static OpenApiSchema ToOpenApiSchema(this Type type, OpenApiSchemaVisibilityAttribute attribute = null)
        {
            type.ThrowIfNullOrDefault();

            var schema = (OpenApiSchema)null;

            if (type == typeof(JObject))
            {
                schema = typeof(object).ToOpenApiSchema();

                return schema;
            }

            if (type == typeof(JToken))
            {
                schema = typeof(object).ToOpenApiSchema();

                return schema;
            }

            var unwrappedValueType = Nullable.GetUnderlyingType(type);
            if (!unwrappedValueType.IsNullOrDefault())
            {
                schema = unwrappedValueType.ToOpenApiSchema();
                schema.Nullable = true;

                return schema;
            }

            schema = new OpenApiSchema()
                         {
                             Type = type.ToDataType(),
                             Format = type.ToDataFormat()
                         };

            if (!attribute.IsNullOrDefault())
            {
                var visibility = new OpenApiString(attribute.Visibility.ToDisplayName());

                schema.Extensions.Add("x-ms-visibility", visibility);
            }

            if (type.IsSimpleType())
            {
                return schema;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                schema.AdditionalProperties = type.GetGenericArguments()[1].ToOpenApiSchema();

                return schema;
            }

            if (type.IsOpenApiArray())
            {
                schema.Type = "array";
                schema.Items = (type.GetElementType() ?? type.GetGenericArguments()[0]).ToOpenApiSchema();

                return schema;
            }

            var properties = type.GetProperties()
                                 .Where(p => !p.ExistsCustomAttribute<JsonIgnoreAttribute>());
            foreach (var property in properties)
            {
                var visiblity = property.GetCustomAttribute<OpenApiSchemaVisibilityAttribute>(inherit: false);
                var propertyName = property.GetJsonPropertyName();

                schema.Properties[propertyName] = property.PropertyType.ToOpenApiSchema(visiblity);
            }

            return schema;
        }
    }
}