using HandleDependenciesAPI.Models;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;

namespace HandleDependenciesAPI.Controllers
{
  
    public class DependenciesController : ApiController
    {
        private static HashSet<string> packageCache;

        public DependenciesController()
        {
            packageCache = new HashSet<string>();
        }
        public async Task<Node> Get(string packageName="", string packageVersion = "")
        {
            try
            {
                if (string.IsNullOrEmpty(packageName)) throw new ApplicationException("Package name can't be empty");
                Package package = new Package() { Name = packageName, Version = String.IsNullOrEmpty(packageVersion) ? "latest" : packageVersion  };
                Node result = await GetDependencyObjectAsync(package, new Node() { Package = package });
                if (result == null) throw new ApplicationException("Package not found");               
                return result;  
            }
            catch (ApplicationException ex)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message)); 
            }
            catch (Exception)
            {
                //1- Write exception to log
                //2 - Display an error message
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An error occured"));
            }
        }     
       

        /// <summary>
        /// A recursive method which receives a package and a tree 
        /// and returns the whole tree of dependencies
        /// </summary>
        /// <param name="package"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private async Task<Node> GetDependencyObjectAsync(Package package, Node node)
        {

            if (package == null) return node;
            string packageResult = await GetPackageAsync(package);
            if (packageResult == null) return null;
            AddPackageToCache(package);
            node.Dependencies = GetDependenciesList(packageResult);
            if (node.Dependencies != null)
            {
                var tasks = new List<Task<Node>>();
                foreach (var item in node.Dependencies)
                {
                    if (!packageCache.Contains(item.Package.GetIdentity()))
                        tasks.Add(Task.Run(() => GetDependencyObjectAsync(item.Package, item)));
                };
                await Task.WhenAll(tasks.ToArray());
            }
            return node;
        }             

        /// <summary>
        /// Fetches the package json from npm registry
        /// </summary>
        /// <param name="package">An object which contains the name and vertsion of the package</param>
        /// <returns></returns>
        private async Task<string> GetPackageAsync(Package package)
        {
            string responseContent = null;
            HttpClient client = new HttpClient();
            string url = GetUrl(package);
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
                responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }

        /// <summary>
        /// Inserts the dependencies pairs (method": "~1.0.0","reducible": "~1.0.1" etc into a list of Node objects)
        /// </summary>
        /// <param name="packageResult">The json string which contains all the package information</param>
        /// <param name="nodeList">The list of Nodes</param>
        /// <returns></returns>
        private List<Node> GetDependenciesList(string packageResult)
        {
            if (packageResult == null) return null;
            Dictionary<string, object> resultList =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(packageResult);
            if (!resultList.ContainsKey("dependencies")) return null;
            if (resultList["dependencies"] == null) return null;
            Dictionary<string, string> dependencies =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(resultList["dependencies"].ToString());
            if (dependencies == null) return null;
            List<Node> list = new List<Node>();
            foreach (var item in dependencies)
            {
                Package package = new Package() { Name = item.Key, Version = item.Value };
                list.Add(new Node() { Package = package });
            }
            return list;
        }
        private void AddPackageToCache(Package package)
        {
            if (!packageCache.Contains(package.GetIdentity()))
                packageCache.Add(package.Name + package.Version);
        }
        private string GetVersion(string version)
        {
            if (String.IsNullOrEmpty(version))
                return "latest";
            else
            {
                string pattern = ConfigurationManager.AppSettings["packageVersionRegex"];
                string patternWithPrefix = ConfigurationManager.AppSettings["packageVersionRegexWithPrefix"];
                Regex regex = new Regex(pattern);
                Regex regexWithPrefix = new Regex(patternWithPrefix);
                if (regex.IsMatch(version))
                    return version;
                else if (regexWithPrefix.IsMatch(version))
                    return version.Substring(1);
                else
                    return "latest";
            }
        }
        private string GetUrl(Package package)
        {
            //examples:
            //"babel-core": "^6.3.26",
            //"babel-plugin-add-module-exports": "~0.1.2",
            //"benchmark": "github:bestiejs/benchmark.js",      
            string baseApiUrl = ConfigurationManager.AppSettings["baseNpmApiUrl"];           
            return string.Format(baseApiUrl, package.Name, GetVersion(package.Version));
        }
    }
}

