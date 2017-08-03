using Fastnet.Core.Web.Controllers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core.Web
{
    internal class JsonContent : StringContent
    {
        public JsonContent(object obj) : base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
        {

        }
    }
    public class WebApiClient : HttpClient
    {
        protected ILogger log;
        public WebApiClient(string url)
        {
            BaseAddress = new Uri(url);
            DefaultRequestHeaders.Clear();
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public WebApiClient(string url, ILogger logger): this(url)
        {
            this.log = logger;
        }
        public WebApiClient(string url, ILoggerFactory loggerFactory): this(url)
        {
            this.log = loggerFactory.CreateLogger(this.GetType().Name);
        }
        protected async Task<T> GetDataResultObject<T>(string query)
        {
            var response = await GetAsync(query);
            if (response != null)
            {
                var json = await response.Content.ReadAsStringAsync();
                DataResult dr = JsonConvert.DeserializeObject<DataResult>(json);
                if (dr.success)
                {
                    object data = dr.data;
                    return JsonConvert.DeserializeObject<T>(data.ToString());
                }
                else
                {
                    if(!string.IsNullOrWhiteSpace(dr.exceptionMessage))
                    {
                        log.LogError($"{this.BaseAddress}/{query} failed with server exception: {dr.exceptionMessage}");
                    }
                    else
                    {
                        log.LogWarning($"{this.BaseAddress}/{query} failed with message: {(dr.message ?? "(no message)")}");
                    }
                }
                return default(T);
            }
            return default(T);
        }
        protected new async Task<HttpResponseMessage> GetAsync(string query)
        {
            try
            {
                var response = await base.GetAsync(query);
                if (!response.IsSuccessStatusCode)
                {
                    var msg = $"{this.BaseAddress}/{query} failed with status code: {response.StatusCode}";
                    log.LogError(msg);
                }
                return response;
            }
            catch (Exception xe)
            {
                log.LogError($"{this.BaseAddress}/{query} failed with exception: {xe.Message}");
                //throw;
            }
            return null;
        }
        /// <summary>
        /// Post an object without any returing data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual async Task PostJsonAsync<T>(string query, T obj)
        {
            try
            {
                var content = new JsonContent(obj);
                var response = await PostAsync(query, content);
                if (!response.IsSuccessStatusCode)
                {
                    var msg = $"Post: {this.BaseAddress}{query} failed with status code: {response.StatusCode}";
                    log.LogError(msg);
                }
                //return response;
            }
            catch (Exception xe)
            {
                log.LogError($"Post: {this.BaseAddress}{query} failed with exception: {xe.Message}");
                //throw;
            }
        }
        /// <summary>
        /// Post an object (ST) with returning data (RT)
        /// </summary>
        /// <typeparam name="ST"></typeparam>
        /// <typeparam name="RT"></typeparam>
        /// <param name="query"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual async Task<RT> PostJsonAsync<ST, RT>(string query, ST obj)
        {
            try
            {
                var content = new JsonContent(obj);
                var response = await PostAsync(query, content);
                if (!response.IsSuccessStatusCode)
                {
                    var msg = $"Post: {this.BaseAddress}{query} failed with status code: {response.StatusCode}";
                    log.LogError(msg);
                    return default(RT);
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<RT>(json);
                }
                //return response;
            }
            catch (Exception xe)
            {
                log.LogError($"Post: {this.BaseAddress}{query} failed with exception: {xe.Message}");
                //throw;
            }
            return default(RT);
        }
    }
}
