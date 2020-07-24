using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HandleDependenciesAPI.Models
{
    public class Package
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string GetIdentity()
        {
            return this.Name + this.Version;
        }
    }
}