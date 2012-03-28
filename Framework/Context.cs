using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using Plywood.Indexes;

namespace Plywood
{
    public class Context : IIndexableEntity
    {
        public Guid Key
        {
            get
            {
                return this.Hierarchy.Key;
            }
        }

        public string Name { get; set; }
        public ContextHierarchy Hierarchy { get { return new ContextHierarchy(this.Name); } }
        public Dictionary<string, string> Tags { get; set; }

        /// <example>
        /// Some.Context.Path
        /// -> /ci/../index-entry
        ///    /c/{Some}/ci/../index-entry
        ///    /c/{Some.Context}/ci/../index-entry
        /// </example>
        public IEnumerable<string> GetIndexEntries()
        {
            var indexKey = string.Format(
                "{0}-{1}",
                Hashing.CreateHash(this.Name), 
                Utils.Indexes.EncodeText(this.Name));

            // TODO: Create better tokeniser that splits based on case change or "."
            var tokens = (new SimpleTokeniser()).Tokenise(this.Name).ToList();

            yield return string.Format("ci/e/{0}", indexKey);
            foreach (var token in tokens)
            {
                yield return string.Format("ci/t/{0}/{1}", Indexes.IndexEntries.GetTokenHash(token), indexKey);
            }

            ContextHierarchy currentHierarchy = this.Hierarchy.Parent;
            while (currentHierarchy != null)
            {
                yield return string.Format(
                    "c/{0}/ci/e/{1}",
                    Utils.Indexes.EncodeGuid(currentHierarchy.Key),
                    indexKey);

                foreach (var token in tokens)
                {
                    yield return string.Format(
                        "c/{0}/ci/t/{1}/{2}",
                        Utils.Indexes.EncodeGuid(currentHierarchy.Key),
                        Indexes.IndexEntries.GetTokenHash(token),
                        indexKey);
                }

                currentHierarchy = currentHierarchy.Parent;
            }
        }
    }
}
