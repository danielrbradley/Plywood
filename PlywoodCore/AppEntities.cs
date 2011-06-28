using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;

namespace Plywood
{
    public class App
    {

        #region Constructors

        public App()
        {
            Key = Guid.NewGuid();
            MajorVersion = "0.1";
            Tags = new Dictionary<string, string>();
        }

        public App(string source)
            : this(App.Parse(source)) { }

        public App(Stream source)
            : this(App.Parse(source)) { }

        public App(TextReader source)
            : this(App.Parse(source)) { }

        public App(XmlReader source)
            : this(App.Parse(source)) { }

        public App(App other)
        {
            this.DeploymentDirectory = other.DeploymentDirectory;
            this.GroupKey = other.GroupKey;
            this.Key = other.Key;
            this.MajorVersion = other.MajorVersion;
            this.Name = other.Name;
            this.Revision = other.Revision;
            this.Tags = other.Tags;
        }

        #endregion

        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public string DeploymentDirectory { get; set; }
        public string MajorVersion { get; set; }
        public int Revision { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public Stream Serialise()
        {
            return App.Serialise(this);
        }

        #region Static Serialisation

        public static App Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising app.", ex);
            }

            if (!ValidateAppXml(doc))
                throw new DeserialisationException("Serialised app xml is not valid.");

            Guid key, groupKey;
            int revision;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised app key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised app group key is not a valid guid.");
            if (!int.TryParse(doc.Root.Element("revision").Value, out revision))
                throw new DeserialisationException("Serialised app revision is not a valid integer.");

            var app = new App()
            {
                Key = key,
                GroupKey = groupKey,
                Name = doc.Root.Element("name").Value,
                DeploymentDirectory = doc.Root.Element("deploymentDirectory").Value,
                MajorVersion = doc.Root.Element("majorVersion").Value,
                Revision = revision,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                app.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }

            return app;
        }

        public static App Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static App Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static App Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Stream Serialise(App app)
        {
            if (app == null)
                throw new ArgumentNullException("app", "App cannot be null.");
            if (!Validation.IsNameValid(app.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");
            if (!Validation.IsDirectoryNameValid(app.DeploymentDirectory))
                throw new ArgumentException("Deployment directory must be a valid directory name.");
            if (!Validation.IsMajorVersionValid(app.MajorVersion))
                throw new ArgumentException("Major version must be numbers separated by '.'");
            if (app.Revision < 0)
                throw new ArgumentOutOfRangeException("app.Revision", app.Revision, "App revision must be a positive integer.");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("app",
                    new XAttribute("key", app.Key),
                    new XElement("groupKey", app.GroupKey),
                    new XElement("name", app.Name),
                    new XElement("deploymentDirectory", app.DeploymentDirectory),
                    new XElement("majorVersion", app.MajorVersion),
                    new XElement("revision", app.Revision),
                    new XElement("tags")));

            if (app.Tags != null && app.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    app.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static bool ValidateAppXml(XDocument appDoc)
        {
            bool valid = true;
            appDoc.Validate(Schemas, (o, e) =>
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
                            schemas = new XmlSchemaSet();
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.App.xsd"))
                            {
                                schemas.Add("", XmlReader.Create(stream));
                            }
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

    public class AppList
    {
        public Guid GroupKey { get; set; }
        public IEnumerable<AppListItem> Apps { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class AppListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
