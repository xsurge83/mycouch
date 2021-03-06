﻿using System;
using System.Collections.Generic;
using MyCouch.Core;
using MyCouch.Schemes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MyCouch.Serialization
{
    public class SerializationContractResolver : DefaultContractResolver
    {
        protected readonly IEntityReflector EntityReflector;

        public SerializationContractResolver(IEntityReflector entityReflector)
            : base(true)
        {
            EntityReflector = entityReflector;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (type == typeof(BulkResponse.Row) || (type.IsGenericType && typeof(ViewQueryResponse<>.Row) == type.GetGenericTypeDefinition()))
                return base.CreateProperties(type, memberSerialization);

            var props = base.CreateProperties(type, memberSerialization);
            int? idRank = null, revRank = null;
            JsonProperty id = null, rev = null;

            foreach (var prop in props)
            {
                var tmpRank = EntityReflector.IdMember.GetMemberRankingIndex(type, prop.PropertyName);
                if (tmpRank != null)
                {
                    if (idRank == null || tmpRank < idRank)
                    {
                        idRank = tmpRank;
                        id = prop;
                    }

                    continue;
                }

                tmpRank = EntityReflector.RevMember.GetMemberRankingIndex(type, prop.PropertyName);
                if (tmpRank != null)
                {
                    if (revRank == null || tmpRank < revRank)
                    {
                        revRank = tmpRank;
                        rev = prop;
                    }

                    continue;
                }
            }

            if (id != null)
                id.PropertyName = "_id";

            if (rev != null)
                rev.PropertyName = "_rev";

            return props;
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            return base.ResolvePropertyName(propertyName.ToCamelCase());
        }
    }
}