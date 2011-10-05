using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Indexes;

namespace Plywood
{
    public class RolePackage : IIndexableEntity
    {
        public RolePackage()
        {
        }

        public Guid TargetKey { get; set; }
        public string TargetName { get; set; }
        public Guid AppKey { get; set; }
        public string AppName { get; set; }
        public string AppDeploymentDirectory { get; set; }

        public IEnumerable<string> GetIndexEntries()
        {
            var entries = new List<string>();
            entries.AddRange(GetTargetIndexEntries());
            entries.AddRange(GetAppIndexEntries());
            return entries;
        }

        private IEnumerable<string> GetTargetIndexEntries()
        {
            // Target Index: /t/{t-guid}/ai/{token/global}/{app-name-hash}-{app-guid}-{app-name}-{app-deployment-dir}
            var filename = string.Format(
                "{0}-{1}-{2}-{3}",
                Hashing.CreateHash(this.AppName),
                Utils.Indexes.EncodeGuid(this.AppKey),
                Utils.Indexes.EncodeText(this.AppName),
                Utils.Indexes.EncodeText(this.AppDeploymentDirectory));

            var tokens = (new SimpleTokeniser()).Tokenise(this.AppName).ToList();
            var entries = new List<string>(tokens.Count() + 1);

            entries.Add(
                string.Format(
                "t/{0}/ai/e/{1}", 
                Utils.Indexes.EncodeGuid(this.TargetKey), 
                filename));

            entries.AddRange(
                tokens.Select(token =>
                    string.Format(
                        "t/{0}/ai/t/{1}/{2}", 
                        Utils.Indexes.EncodeGuid(this.TargetKey), 
                        Indexes.IndexEntries.GetTokenHash(token), 
                        filename)));

            return entries;
        }

        private IEnumerable<string> GetAppIndexEntries()
        {
            // App Index: /a/{a-guid}/ti/{token/global}/{target-name-hash}-{target-guid}-{target-name}
            var filename = string.Format(
                "{0}-{1}-{2}",
                Hashing.CreateHash(this.TargetName),
                Utils.Indexes.EncodeGuid(this.TargetKey),
                Utils.Indexes.EncodeText(this.TargetName));

            var tokens = (new SimpleTokeniser()).Tokenise(this.TargetName).ToList();
            var entries = new List<string>(tokens.Count() + 1);

            entries.Add(
                string.Format(
                "a/{0}/ti/e/{1}", 
                Utils.Indexes.EncodeGuid(this.AppKey), 
                filename));

            entries.AddRange(
                tokens.Select(token =>
                    string.Format(
                        "a/{0}/ti/t/{1}/{2}", 
                        Utils.Indexes.EncodeGuid(this.AppKey), 
                        Indexes.IndexEntries.GetTokenHash(token), 
                        filename)));

            return entries;
        }
    }
}
