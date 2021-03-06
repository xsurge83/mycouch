﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MyCouch.Schemes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MyCouch.Serialization
{
    public class MyCouchSerializer : ISerializer
    {
        protected readonly IEntityReflector EntityReflector;
        protected readonly JsonSerializer InternalSerializer;

        public MyCouchSerializer(IEntityReflector entityReflector)
        {
            EntityReflector = entityReflector;
            InternalSerializer = JsonSerializer.Create(CreateSettings(new SerializationContractResolver(EntityReflector)));
        }

        protected virtual JsonSerializerSettings CreateSettings(IContractResolver contractResolver)
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = contractResolver,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public virtual string Serialize<T>(T item) where T : class
        {
            var content = new StringBuilder();
            using (var textWriter = new StringWriter(content))
            {
                InternalSerializer.Serialize(textWriter, item);
            }
            return content.ToString();
        }

        public virtual string SerializeEntity<T>(T entity) where T : class
        {
            var content = new StringBuilder();
            using (var textWriter = new StringWriter(content))
            {
                using (var jsonWriter = CreateEntityWriter(textWriter))
                {
                    jsonWriter.WriteDocHeaderFor(entity);
                    InternalSerializer.Serialize(jsonWriter, entity);
                }
            }
            return content.ToString();
        }

        public virtual T Deserialize<T>(string data) where T : class
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;

            using (var reader = new StringReader(data))
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    return InternalSerializer.Deserialize<T>(jsonReader);
                }
            }
        }

        public virtual T Deserialize<T>(Stream data) where T : class
        {
            if (data == null || data.Length < 1)
                return null;

            using (var reader = new StreamReader(data, MyCouchRuntime.DefaultEncoding))
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    return InternalSerializer.Deserialize<T>(jsonReader);
                }
            }
        }

        public virtual void PopulateFailedResponse<T>(T response, Stream data) where T : Response
        {
            var mappings = new Dictionary<string, Action<JsonTextReader>>
            {
                {"error", jr => response.Error = jr.Value.ToString()},
                {"reason", jr => response.Reason = jr.Value.ToString()}
            };
            Map(data, mappings);
        }

        public virtual void PopulateBulkResponse(BulkResponse response, Stream data)
        {
            using (var sr = new StreamReader(data))
            {
                using (var jr = new JsonTextReader(sr) { CloseInput = false })
                {
                    response.Rows = InternalSerializer.Deserialize<BulkResponse.Row[]>(jr);
                }
            }
        }

        public virtual void PopulateCopyDocumentResponse(CopyDocumentResponse response, Stream data)
        {
            var mappings = new Dictionary<string, Action<JsonTextReader>>
            {
                {"id", jr => response.Id = jr.Value.ToString()},
                {"rev", jr => response.Rev = jr.Value.ToString()}
            };
            Map(data, mappings);
        }

        public virtual void PopulateReplaceDocumentResponse(ReplaceDocumentResponse response, Stream data)
        {
            var mappings = new Dictionary<string, Action<JsonTextReader>>
            {
                {"id", jr => response.Id = jr.Value.ToString()},
                {"rev", jr => response.Rev = jr.Value.ToString()}
            };
            Map(data, mappings);
        }

        public virtual void PopulateDocumentResponse<T>(T response, Stream data) where T : DocumentResponse
        {
            var mappings = new Dictionary<string, Action<JsonTextReader>>
            {
                {"id", jr => response.Id = jr.Value.ToString()},
                {"rev", jr => response.Rev = jr.Value.ToString()}
            };
            Map(data, mappings);
        }

        public virtual void PopulateViewQueryResponse<T>(ViewQueryResponse<T> response, Stream data) where T : class
        {
            var mappings = new Dictionary<string, Action<JsonTextReader>>
            {
                {"total_rows", jr => response.TotalRows = (long)jr.Value},
                {"update_seq", jr => response.UpdateSeq = (long)jr.Value},
                {"offset", jr => response.OffSet = (long)jr.Value},
                {"rows", jr =>
                {
                    if (response is ViewQueryResponse<string>)
                        response.Rows = YieldViewQueryRowsOfString(jr).ToArray() as ViewQueryResponse<T>.Row[];
                    else if (response is ViewQueryResponse<string[]>)
                        response.Rows = YieldViewQueryRowsOfStrings(jr).ToArray() as ViewQueryResponse<T>.Row[];
                    else
                        response.Rows = InternalSerializer.Deserialize<ViewQueryResponse<T>.Row[]>(jr);
                }},
            };
            Map(data, mappings);
        }

        protected virtual void Map(Stream data, IDictionary<string, Action<JsonTextReader>> mappings)
        {
            var numOfHandlersProcessed = 0;

            using (var sr = new StreamReader(data))
            {
                using (var jr = new JsonTextReader(sr) { CloseInput = false })
                {
                    while (jr.Read())
                    {
                        if(numOfHandlersProcessed == mappings.Count)
                            break;

                        if (jr.TokenType != JsonToken.PropertyName)
                            continue;

                        var propName = jr.Value.ToString();
                        if (!mappings.ContainsKey(propName))
                            continue;

                        if (!jr.Read())
                            break;
                        
                        mappings[propName](jr);

                        numOfHandlersProcessed++;
                    }
                }
            }
        }

        protected IEnumerable<ViewQueryResponse<string>.Row> YieldViewQueryRowsOfString(JsonReader jr)
        {
            return YieldViewQueryRows<string>(jr, (row, jw, sb) =>
            {
                jw.WriteToken(jr, true);
                row.Value = sb.ToString();
            });
        }

        protected IEnumerable<ViewQueryResponse<string[]>.Row> YieldViewQueryRowsOfStrings(JsonReader jr)
        {
            var rowValues = new List<string>();

            return YieldViewQueryRows<string[]>(jr, (row, jw, sb) =>
            {
                var valueStartDepth = jr.Depth;

                while (jr.Read() && !(jr.TokenType == JsonToken.EndArray && jr.Depth == valueStartDepth))
                {
                    jw.WriteToken(jr, true);
                    rowValues.Add(sb.ToString());
                    sb.Clear();
                }

                row.Value = rowValues.ToArray();
                rowValues.Clear();
            });
        }

        protected IEnumerable<ViewQueryResponse<T>.Row> YieldViewQueryRows<T>(JsonReader jr, Action<ViewQueryResponse<T>.Row, JsonWriter, StringBuilder> onVisitValue) where T : class
        {
            if (jr.TokenType != JsonToken.StartArray)
                yield break;

            var row = new ViewQueryResponse<T>.Row();
            var startDepth = jr.Depth;
            var sb = new StringBuilder();
            var hasMappedId = false;
            var hasMappedKey = false;
            var hasMappedValue = false;

            using (var sw = new StringWriter(sb))
            {
                using (var jw = new JsonTextWriter(sw))
                {
                    while (jr.Read() && !(jr.TokenType == JsonToken.EndArray && jr.Depth == startDepth))
                    {
                        if (jr.TokenType != JsonToken.PropertyName)
                            continue;

                        var propName = jr.Value.ToString().ToLower();
                        if (propName == "id")
                        {
                            if (!jr.Read())
                                break;
                            row.Id = jr.Value.ToString();
                            hasMappedId = true;
                        }
                        else if (propName == "key")
                        {
                            if (!jr.Read())
                                break;
                            row.Key = jr.Value.ToString();
                            hasMappedKey = true;
                        }
                        else if (propName == "value")
                        {
                            if (!jr.Read())
                                break;

                            onVisitValue(row, jw, sb);
                            sb.Clear();
                            hasMappedValue = true;
                        }
                        else
                            continue;

                        if (hasMappedId && hasMappedKey && hasMappedValue)
                        {
                            hasMappedId = hasMappedKey = hasMappedValue = false;
                            yield return row;
                            row = new ViewQueryResponse<T>.Row();
                        }
                    }
                }
            }
        }

        protected virtual SerializationEntityWriter CreateEntityWriter(TextWriter textWriter)
        {
            return new SerializationEntityWriter(textWriter);
        }
    }
}