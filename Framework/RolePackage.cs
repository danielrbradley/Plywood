using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Indexes;

namespace Plywood
{
    internal class RolePackage : IIndexableEntity
    {
        public RolePackage()
        {
        }

        public RolePackage(Role role, Package package)
        {
            this.RoleKey = role.Key;
            this.RoleName = role.Name;
            this.PackageDeploymentDirectory = package.DeploymentDirectory;
            this.PackageKey = package.Key;
            this.PackageName = package.Name;
        }

        public Guid RoleKey { get; set; }
        public string RoleName { get; set; }
        public Guid PackageKey { get; set; }
        public string PackageName { get; set; }
        public string PackageDeploymentDirectory { get; set; }

        public IEnumerable<string> GetIndexEntries()
        {
            var entries = new List<string>();
            entries.AddRange(GetRoleIndexEntries());
            entries.AddRange(GetPackageIndexEntries());
            return entries;
        }

        private IEnumerable<string> GetRoleIndexEntries()
        {
            // Role Package Index: /r/{role-guid}/pi/{token/global}/{pkg-name-hash}-{pkg-guid}-{pkg-name}-{pkg-deployment-dir}
            var filename = string.Format(
                "{0}-{1}-{2}-{3}",
                Hashing.CreateHash(this.PackageName),
                Utils.Indexes.EncodeGuid(this.PackageKey),
                Utils.Indexes.EncodeText(this.PackageName),
                Utils.Indexes.EncodeText(this.PackageDeploymentDirectory));

            var tokens = (new SimpleTokeniser()).Tokenise(this.PackageName).ToList();
            var entries = new List<string>(tokens.Count() + 1);

            entries.Add(
                string.Format(
                "r/{0}/pi/e/{1}", 
                Utils.Indexes.EncodeGuid(this.RoleKey), 
                filename));

            entries.AddRange(
                tokens.Select(token =>
                    string.Format(
                        "r/{0}/pi/t/{1}/{2}", 
                        Utils.Indexes.EncodeGuid(this.RoleKey), 
                        Indexes.IndexEntries.GetTokenHash(token), 
                        filename)));

            return entries;
        }

        private IEnumerable<string> GetPackageIndexEntries()
        {
            // Package Role Index: /p/{pkg-guid}/ri/{token/global}/{role-name-hash}-{role-guid}-{role-name}
            var filename = string.Format(
                "{0}-{1}-{2}",
                Hashing.CreateHash(this.RoleName),
                Utils.Indexes.EncodeGuid(this.RoleKey),
                Utils.Indexes.EncodeText(this.RoleName));

            var tokens = (new SimpleTokeniser()).Tokenise(this.RoleName).ToList();
            var entries = new List<string>(tokens.Count() + 1);

            entries.Add(
                string.Format(
                "p/{0}/ri/e/{1}", 
                Utils.Indexes.EncodeGuid(this.PackageKey), 
                filename));

            entries.AddRange(
                tokens.Select(token =>
                    string.Format(
                        "p/{0}/ri/t/{1}/{2}", 
                        Utils.Indexes.EncodeGuid(this.PackageKey), 
                        Indexes.IndexEntries.GetTokenHash(token), 
                        filename)));

            return entries;
        }
    }
}
