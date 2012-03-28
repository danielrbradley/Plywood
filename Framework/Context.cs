using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Plywood
{
    public class Context : IIndexableEntity
    {
        public Guid Key
        {
            get
            {
                return Context.GetNamespaceKey(this.Hierarchy.FullName);
            }
        }

        public ContextHierarchy Hierarchy { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public IEnumerable<string> GetIndexEntries()
        {
            throw new NotImplementedException();
        }

        public static Guid GetNamespaceKey(string name)
        {
            var md5 = MD5.Create();
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(name);
            var hash = md5.ComputeHash(inputBytes);
            return new Guid(hash);
        }
    }
}
