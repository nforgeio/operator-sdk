// -----------------------------------------------------------------------------
// FILE:	    TypeExtensions.cs
// CONTRIBUTOR: NEONFORGE Team
// COPYRIGHT:   Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using k8s;
using k8s.Models;

using Neon.Roslyn;

namespace Neon.Operator.Analyzers
{
    public static class TypeExtensions
    {
        public static bool IsSimpleType(this Type type)
        {
            if (type.IsPrimitive)
            {
                return true;
            }

            if (type.Equals(typeof(string))
                || type.Equals(typeof(Guid))
                || type.Equals(typeof(TimeSpan))
                || type.Equals(typeof(DateTime))
                || type.Equals(typeof(DateTimeOffset))
                )
            {
                return true;
            }

            if (type.IsGenericType
                && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (type.GetGenericArguments().FirstOrDefault().IsSimpleType())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEnumerableType(this Type type, out Type typeParameter)
        {
            var genericDef = (RoslynType)type.GetGenericTypeDefinition();
            
            if (genericDef.IsAssignableTo(typeof(IEnumerable<>))
                || genericDef.IsAssignableTo(typeof(IList<>)))
            {
                typeParameter = type.GetGenericArguments().First();
            }
            else
            {
                typeParameter = type
                    .GetInterfaces()
                    .Where(t => t.IsGenericType
                            && (t.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>))
                                || t.GetGenericTypeDefinition().Equals(typeof(IList<>))))
                    .Select(t => t.GetGenericArguments().FirstOrDefault())
                    .FirstOrDefault();
            }

            if (typeParameter != null)
            {
                return true;
            }

            return false;
        }

        public static V1JSONSchemaProps ToJsonSchemaProps(this Dictionary<object, object> value)
        {
            var result = new V1JSONSchemaProps();

            if (value.ContainsKey("additionalItems"))
            {
                result.AdditionalItems = value["additionalItems"];
            }

            if (value.ContainsKey("additionalProperties"))
            {
                result.AdditionalProperties = value["additionalProperties"];
            }

            if (value.ContainsKey("description"))
            {
                result.Description = (string)value["description"];
            }
            //result.DefaultProperty = value["defaultProperty"];
            //result.Definitions = value["definitions"];
            //result.Dependencies = value["dependencies"];

            if (value.ContainsKey("enum"))
            {
                result.EnumProperty = (List<object>)value["enum"];
            }
            //result.Example = value["example"];
            //result.ExclusiveMaximum = value["exclusiveMaximum"];
            //result.ExclusiveMinimum = value["exclusiveMinimum"];
            //result.ExternalDocs = value["externalDocs"];

            if (value.ContainsKey("format"))
            {
                result.Format = (string)value["format"];
            }

            if (value.ContainsKey("id"))
            {
                result.Id = (string)value["id"];
            }

            if (value.ContainsKey("items"))
            {
                var items = value["items"];
                if (items.GetType() == typeof(Dictionary<object, object>))
                {
                    result.Items = ((Dictionary<object, object>)items).ToJsonSchemaProps();
                }
                else if (items.GetType() == typeof(List<object>))
                {
                    var itemsResult = new List<V1JSONSchemaProps>();

                    foreach (var p in (List<object>)items)
                    {
                        itemsResult.Add(((Dictionary<object, object>)p).ToJsonSchemaProps());
                    }
                    result.Items = itemsResult;
                }
            }

            if (value.ContainsKey("maximum"))
            {
                result.Maximum = double.Parse((string)value["maximum"]);
            }

            if (value.ContainsKey("maxItems"))
            {
                result.MaxItems = long.Parse((string)value["maxItems"]);
            }

            if (value.ContainsKey("maxLength"))
            {
                result.MaxLength = long.Parse((string)value["maxLength"]);
            }

            if (value.ContainsKey("maxProperties"))
            {
                if (value["maxProperties"].GetType() == typeof(byte))
                {
                    result.MaxProperties = (int)((byte)value["maxProperties"]);
                }
                else if (long.TryParse((string)value["maxProperties"], out var mp))
                {
                    result.MaxProperties = mp;
                }
            }

            if (value.ContainsKey("minimum"))
            {
                result.Minimum = double.Parse((string)value["minimum"]);
            }

            if (value.ContainsKey("minItems"))
            {
                if (value["minItems"].GetType() == typeof(byte))
                {
                    result.MinItems = (int)((byte)value["minItems"]);
                }
                else if (long.TryParse((string)value["minItems"], out var mi))
                {
                    result.MinItems = mi;
                }
            }

            if (value.ContainsKey("minLength"))
            {
                if (value["minLength"].GetType() == typeof(byte))
                {
                    result.MinLength = (int)((byte)value["minLength"]);
                    }
                else if (long.TryParse((string)value["minLength"], out var ml))
                {
                    result.MinLength = ml;
                }
            }

            if (value.ContainsKey("minProperties"))
            {
                result.MinProperties = long.Parse((string)value["minProperties"]);
            }

            if (value.ContainsKey("multipleOf"))
            {
                result.MultipleOf = double.Parse((string)value["multipleOf"]);
            }

            if (value.ContainsKey("nullable"))
            {
                result.Nullable = bool.Parse((string)value["nullable"]);
            }

            if (value.ContainsKey("pattern"))
            {
                result.Pattern = (string)value["pattern"];
            }

            if (value.ContainsKey("required"))
            {
                result.Required = ((List<object>)value["required"]).Select(x => (string)x).ToList();
            }

            if (value.ContainsKey("title"))
            {
                result.Title = (string)value["title"];
            }

            if (value.ContainsKey("type"))
            {
                result.Type = (string)value["type"];
            }

            if (value.ContainsKey("uniqueItems"))
            {
                result.UniqueItems = bool.Parse((string)value["uniqueItems"]);
            }

            if (value.ContainsKey("properties"))
            {
                result.Properties = new Dictionary<string, V1JSONSchemaProps>();

                var props = value["properties"];

                foreach (var prop in (Dictionary<object, object>)props)
                {
                    result.Properties.Add((string)prop.Key, ((Dictionary<object, object>)prop.Value).ToJsonSchemaProps());
                }
            }

            //result.Not = value["additionalItems"];
            //result.OneOf = value["additionalItems"];
            //result.PatternProperties = value["additionalItems"];
            //result.RefProperty = value["additionalItems"];
            //result.Schema = value["additionalItems"];
            //result.XKubernetesEmbeddedResource = value["additionalItems"];
            //result.XKubernetesIntOrString = value["additionalItems"];
            //result.XKubernetesListMapKeys = value["additionalItems"];
            //result.XKubernetesListType = value["additionalItems"];
            //result.XKubernetesMapType = value["additionalItems"];
            //result.XKubernetesPreserveUnknownFields = value["additionalItems"];
            //result.XKubernetesValidations = value["additionalItems"];

            if (value.ContainsKey("allOf"))
            {
                result.AllOf = new List<V1JSONSchemaProps>();

                foreach (var a in (IEnumerable<Dictionary<object, object>>)value["allOf"])
                {
                    result.AllOf.Add(a.ToJsonSchemaProps());
                }
            }

            if (value.ContainsKey("anyOf"))
            {
                result.AnyOf = new List<V1JSONSchemaProps>();

                foreach (var a in (IEnumerable<object>)value["anyOf"])
                {
                    result.AnyOf.Add(((Dictionary<object, object>)a).ToJsonSchemaProps());
                }
            }

            if (value.ContainsKey("items"))
            {
                result.Items = ((Dictionary<object, object>)value["items"]).ToJsonSchemaProps();
            }

            return result;
        }

        public static string GetGlobalTypeName(this Type t)
        {
            var sb = new StringBuilder();
            sb.Append("global::");

            if (!t.IsGenericType)
            {
                sb.Append(t.FullName);
                return sb.ToString();
            }

            sb.Append(t.FullName.Substring(0, t.FullName.IndexOf('`')));
            sb.Append('<');
            bool appendComma = false;
            foreach (Type arg in t.GetGenericArguments())
            {
                if (appendComma)
                {
                    sb.Append(", ");
                }

                sb.Append(GetGlobalTypeName(arg));
                appendComma = true;
            }
            sb.Append('>');
            return sb.ToString();
        }
    }
}
