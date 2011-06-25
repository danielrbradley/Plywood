using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;

namespace Plywood
{
    public class App
    {

        #region Constructors

        public App()
        {
            Key = Guid.NewGuid();
            MajorVersion = "0.1";
        }

        public App(string source)
            : base()
        {
            Extend(App.Parse(source));
        }

        public App(Stream source)
            : base()
        {
            Extend(App.Parse(source));
        }

        public App(TextReader source)
            : base()
        {
            Extend(App.Parse(source));
        }

        #endregion

        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public string DeploymentDirectory { get; set; }
        public string MajorVersion { get; set; }
        public int Revision { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        private void Extend(App prototype)
        {
            this.DeploymentDirectory = prototype.DeploymentDirectory;
            this.GroupKey = prototype.GroupKey;
            this.Key = prototype.Key;
            this.MajorVersion = prototype.MajorVersion;
            this.Name = prototype.Name;
            this.Revision = prototype.Revision;
            this.Tags = prototype.Tags;
        }

        public Stream Serialise()
        {
            return App.Serialise(this);
        }

        #region Static Serialisation

        public static App Parse(TextReader source)
        {
            var app = new App();
            var properties = Utils.Serialisation.ReadProperties(source);

            if (!properties.ContainsKey("Key"))
            {
                throw new DeserialisationException("Failed deserialising app: missing property \"Key\"");
            }
            if (!properties.ContainsKey("Name"))
            {
                throw new DeserialisationException("Failed deserialising app: missing property \"Name\"");
            }
            if (!properties.ContainsKey("GroupKey"))
            {
                throw new DeserialisationException("Failed deserialising app: missing property \"GroupKey\"");
            }
            if (!properties.ContainsKey("DeploymentDirectory"))
            {
                throw new DeserialisationException("Failed deserialising app: missing property \"DeploymentDirectory\"");
            }

            Guid key;
            if (!Guid.TryParseExact(properties["Key"], "N", out key))
            {
                throw new DeserialisationException("Failed deserialising app: invalid property value for \"Key\"");
            }
            Guid groupKey;
            if (!Guid.TryParseExact(properties["GroupKey"], "N", out groupKey))
            {
                throw new DeserialisationException("Failed deserialising app: invalid property value for \"GroupKey\"");
            }

            app.Key = key;
            app.Name = properties["Name"];
            app.GroupKey = groupKey;
            app.DeploymentDirectory = properties["DeploymentDirectory"];

            properties.Remove("Key");
            properties.Remove("Name");
            properties.Remove("GroupKey");
            properties.Remove("DeploymentDirectory");

            if (properties.ContainsKey("MajorVersion"))
            {
                app.MajorVersion = properties["MajorVersion"];
                properties.Remove("MajorVersion");
            }

            if (properties.ContainsKey("Revision"))
            {
                int revision;
                if (int.TryParse(properties["Revision"], out revision))
                    app.Revision = revision;
                properties.Remove("Revision");
            }

            app.Tags = properties;

            return app;
        }

        public static App Parse(Stream source)
        {
            return Parse(new StreamReader(source));
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

            if (app.Tags != null)
            {
                if (app.Tags.ContainsKey("Key"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Key\"");
                if (app.Tags.ContainsKey("Name"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Name\"");
                if (app.Tags.ContainsKey("GroupKey"))
                    throw new ArgumentException("Tags cannot use the reserved name \"GroupKey\"");
                if (app.Tags.ContainsKey("DeploymentDirectory"))
                    throw new ArgumentException("Tags cannot use the reserved name \"DeploymentDirectory\"");
                if (app.Tags.ContainsKey("MajorVersion"))
                    throw new ArgumentException("Tags cannot use the reserved name \"MajorVersion\"");
                if (app.Tags.ContainsKey("Revision"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Revision\"");
            }

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("Key", app.Key.ToString("N")),
                new KeyValuePair<string,string>("Name", app.Name),
                new KeyValuePair<string,string>("GroupKey", app.GroupKey.ToString("N")),
                new KeyValuePair<string,string>("DeploymentDirectory", app.DeploymentDirectory),
                new KeyValuePair<string,string>("MajorVersion", app.MajorVersion),
                new KeyValuePair<string,string>("Revision", app.Revision.ToString()),
            };

            if (app.Tags != null)
                values.AddRange(app.Tags.ToList());

            return Serialisation.Serialise(values);
        }

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
