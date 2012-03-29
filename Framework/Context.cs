using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using Plywood.Indexes;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;
using System.Xml.Linq;
using Plywood.Utils;

namespace Plywood
{
    public class Context : IIndexableEntity
    {
        #region Constructors

        public Context()
        {
            this.Tags = new Dictionary<string, string>();
        }

        public Context(string source)
            : this(Context.Parse(source)) { }

        public Context(Stream source)
            : this(Context.Parse(source)) { }

        public Context(TextReader source)
            : this(Context.Parse(source)) { }

        public Context(XmlTextReader source)
            : this(Context.Parse(source)) { }

        public Context(Context other)
        {
            this.Name = other.Name;
            this.Tags = other.Tags;
        }

        #endregion

        /// <summary>
        /// Gets the key identifier of the context.
        /// </summary>
        public Guid Key
        {
            get
            {
                return this.Hierarchy.Key;
            }
        }

        /// <summary>
        /// Gets or sets the name of the context.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the heirachy model for the context name.
        /// </summary>
        public ContextHierarchy Hierarchy { get { return new ContextHierarchy(this.Name); } }

        /// <summary>
        /// Gets or sets the collection of tags and their values of the context.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }

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

        public Stream Serialise()
        {
            return Context.Serialise(this);
        }

        #region Static Serialisation Methods

        public static Stream Serialise(Context context)
        {
            if (context == null)
                throw new ArgumentNullException("context", "Context cannot be null.");
            if (!Validation.IsNameValid(context.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("context",
                    new XElement("name", context.Name),
                    new XElement("tags")));

            if (context.Tags != null && context.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    context.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static Context Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Context Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Context Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Context Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising context.", ex);
            }

            if (!ValidateContextXml(doc))
                throw new DeserialisationException("Serialised context xml is not valid.");

            var context = new Context()
            {
                Name = doc.Root.Element("name").Value,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                context.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }
            else
            {
                context.Tags = new Dictionary<string, string>();
            }

            return context;
        }

        public static bool ValidateContextXml(XDocument contextDoc)
        {
            bool valid = true;
            contextDoc.Validate(Schemas, (o, e) =>
            {
                valid = false;
            });
            return valid;
        }

        public static XmlSchemaSet Schemas
        {
            get
            {
                if (schemas == null)
                {
                    lock (schemasLock)
                    {
                        if (schemas == null)
                        {
                            var newSchemas = new XmlSchemaSet();
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Context.xsd"))
                            {
                                newSchemas.Add("", XmlReader.Create(stream));
                            }
                            schemas = newSchemas;
                        }
                    }
                }
                return schemas;
            }
        }

        private static XmlSchemaSet schemas;
        private static object schemasLock = new object();

        #endregion
    }
}
