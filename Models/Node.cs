using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HandleDependenciesAPI.Models
{
    public class Node
    {
        public Package Package { get; set; }
        public List<Node> Dependencies { get; set; }
    }

}