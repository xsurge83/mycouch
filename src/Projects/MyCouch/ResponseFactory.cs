﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using EnsureThat;

namespace MyCouch
{
    public class ResponseFactory : IResponseFactory
    {
        protected readonly IClient Client;

        public ResponseFactory(IClient client)
        {
            Ensure.That(client, "client").IsNotNull();

            Client = client;
        }

        public virtual DatabaseResponse CreateDatabaseResponse(HttpResponseMessage response)
        {
            return CreateResponse<DatabaseResponse>(response, OnSuccessfulResponseContentMaterializer, OnFailedResponseContentMaterializer);
        }

        public virtual BulkResponse CreateBulkResponse(HttpResponseMessage response)
        {
            return CreateResponse<BulkResponse>(response, OnSuccessfulResponseContentMaterializer, OnFailedResponseContentMaterializer);
        }

        public virtual CopyDocumentResponse CreateCopyDocumentResponse(HttpResponseMessage response)
        {
            return CreateResponse<CopyDocumentResponse>(response, OnSuccessfulResponseContentMaterializer, OnFailedResponseContentMaterializer);
        }

        public virtual ReplaceDocumentResponse CreateReplaceDocumentResponse(HttpResponseMessage response)
        {
            return CreateResponse<ReplaceDocumentResponse>(response, OnSuccessfulResponseContentMaterializer, OnFailedResponseContentMaterializer);
        }

        public virtual JsonDocumentResponse CreateJsonDocumentResponse(HttpResponseMessage response)
        {
            return CreateResponse<JsonDocumentResponse>(response, OnSuccessfulResponseContentMaterializer, OnFailedResponseContentMaterializer);
        }

        public virtual EntityResponse<T> CreateEntityResponse<T>(HttpResponseMessage response) where T : class
        {
            return CreateResponse<EntityResponse<T>>(response, OnSuccessfulResponseContentMaterializer, OnFailedResponseContentMaterializer);
        }

        public virtual JsonViewQueryResponse CreateJsonViewQueryResponse(HttpResponseMessage response)
        {
            return CreateResponse<JsonViewQueryResponse>(response, OnSuccessfulResponseContentMaterializer, OnFailedResponseContentMaterializer);
        }

        public virtual ViewQueryResponse<T> CreateViewQueryResponse<T>(HttpResponseMessage response) where T : class
        {
            return CreateResponse<ViewQueryResponse<T>>(response, OnSuccessfulResponseContentMaterializer, OnFailedResponseContentMaterializer);
        }

        protected virtual T CreateResponse<T>(HttpResponseMessage response, Action<HttpResponseMessage, T> onSuccessfulResponseContentMaterializer, Action<HttpResponseMessage, T> onFailedResponseContentMaterializer) where T : Response, new()
        {
            var result = new T
            {
                RequestUri = response.RequestMessage.RequestUri,
                StatusCode = response.StatusCode,
                RequestMethod = response.RequestMessage.Method
            };

            if (result.IsSuccess)
                onSuccessfulResponseContentMaterializer(response, result);
            else if (!result.IsSuccess)
                onFailedResponseContentMaterializer(response, result);

            return result;
        }

        protected virtual void OnSuccessfulResponseContentMaterializer(HttpResponseMessage response, DatabaseResponse result) { }

        protected virtual void OnSuccessfulResponseContentMaterializer(HttpResponseMessage response, BulkResponse result)
        {
            using (var content = response.Content.ReadAsStreamAsync().Result)
                Client.Serializer.PopulateBulkResponse(result, content);
        }

        protected virtual void OnSuccessfulResponseContentMaterializer(HttpResponseMessage response, CopyDocumentResponse result)
        {
            using (var content = response.Content.ReadAsStreamAsync().Result)
                Client.Serializer.PopulateCopyDocumentResponse(result, content);
        }

        protected virtual void OnSuccessfulResponseContentMaterializer(HttpResponseMessage response, ReplaceDocumentResponse result)
        {
            using (var content = response.Content.ReadAsStreamAsync().Result)
                Client.Serializer.PopulateReplaceDocumentResponse(result, content);
        }

        protected virtual void OnSuccessfulResponseContentMaterializer(HttpResponseMessage response, JsonDocumentResponse result)
        {
            using (var content = response.Content.ReadAsStreamAsync().Result)
            {
                OnSuccessfulResponseContentMaterializer(response.Headers, content, result);

                if (result.RequestMethod == HttpMethod.Get)
                {
                    content.Position = 0;
                    using (var reader = new StreamReader(content, MyCouchRuntime.DefaultEncoding))
                    {
                        result.Content = reader.ReadToEnd();
                    }
                }
            }
        }

        protected virtual void OnSuccessfulResponseContentMaterializer<T>(HttpResponseMessage response, EntityResponse<T> result) where T : class
        {
            using (var content = response.Content.ReadAsStreamAsync().Result)
            {
                OnSuccessfulResponseContentMaterializer(response.Headers, content, result);

                if (result.RequestMethod == HttpMethod.Get)
                {
                    content.Position = 0;
                    result.Entity = Client.Serializer.Deserialize<T>(content);
                }
            }
        }

        protected virtual void OnSuccessfulResponseContentMaterializer<T>(HttpResponseHeaders headers, Stream content, T result) where T : DocumentResponse
        {
            if (result.ContentShouldHaveIdAndRev())
                Client.Serializer.PopulateDocumentResponse(result, content);

            if (result.RequestMethod == HttpMethod.Get)
            {
                if (result.RequestUri.Segments.Any())
                    result.Id = result.RequestUri.Segments.Last();

                var etag = headers.ETag;
                if (etag != null && etag.Tag != null)
                    result.Rev = etag.Tag.Substring(1, etag.Tag.Length - 2);
            }
        }

        protected virtual void OnSuccessfulResponseContentMaterializer<T>(HttpResponseMessage response, ViewQueryResponse<T> result) where T : class
        {
            using(var content = response.Content.ReadAsStreamAsync().Result)
                Client.Serializer.PopulateViewQueryResponse(result, content);
        }
        
        protected virtual void OnFailedResponseContentMaterializer<T>(HttpResponseMessage response, T result) where T : Response
        {
            using (var content = response.Content.ReadAsStreamAsync().Result)
                Client.Serializer.PopulateFailedResponse(result, content);
        }
    }
}