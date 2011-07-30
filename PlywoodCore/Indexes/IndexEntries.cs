using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;

namespace Plywood.Indexes
{
    public class IndexEntries : ControllerBase
    {
        public IndexEntries() : base() { }
        public IndexEntries(ControllerConfiguration context) : base(context) { }

        public void DeleteIndexEntry(IndexEntry indexEntry)
        {
            var paths = GetIndexEntryPaths(indexEntry);
            DeletePaths(paths);
        }

        public void PutIndexEntry(IndexEntry indexEntry)
        {
            var paths = GetIndexEntryPaths(indexEntry);
            PutPaths(paths);
        }

        public void UpdateIndexEntry(IndexEntry oldIndexEntry, IndexEntry newIndexEntry)
        {
            var oldPaths = GetIndexEntryPaths(oldIndexEntry);
            var newPaths = GetIndexEntryPaths(newIndexEntry);

            var deletes = oldPaths.Where(p => !newPaths.Contains(p));
            var puts = newPaths.Where(p => !oldPaths.Contains(p));

            PutPaths(puts);
            DeletePaths(deletes);
        }

        public IndexQueryResult QueryIndex(int maximumRows, string marker, string basePath, IEnumerable<string> tokens)
        {
            IEnumerable<IndexQueryResult> rawResults;
            if (tokens != null && tokens.Any())
            {
                rawResults = QueryIndexes(maximumRows, tokens.Select(t =>
                    {
                        string prefix = string.Format("{0}/t/{1}/", basePath, GetTokenHash(t));
                        return GetIndexQuery(prefix, marker);
                    }));
            }
            else
            {
                string prefix = string.Format("{0}/e/", basePath);
                rawResults = QueryIndexes(maximumRows, new List<IndexQuery>
                {
                    GetIndexQuery(prefix, marker)
                });
            }

            var sorted = rawResults.SelectMany(r => r.Results).OrderBy(r => r.SortHash).Distinct().ToList();
            var limited = sorted.Take(maximumRows).ToList();

            return new IndexQueryResult()
            {
                IsTruncated = sorted.Count > maximumRows || rawResults.Any(r => r.IsTruncated),
                Results = limited,
                NextMarker = limited.Last().SortHash,
            };
        }

        private IndexQuery GetIndexQuery(string prefix, string marker)
        {
            return new IndexQuery()
            {
                Marker = string.Format("{0}{1}", prefix, marker),
                Prefix = prefix,
            };

        }

        #region Internal Methods
        // We could make these internal methods asynchronous to improve performance on updates.

        private class IndexQuery
        {
            public string Marker { get; set; }
            public string Prefix { get; set; }
        }

        private IEnumerable<IndexQueryResult> QueryIndexes(int maximumRows, IEnumerable<IndexQuery> queries)
        {
            try
            {
                return queries.AsParallel().Select(q =>
                    {
                        using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                        {
                            using (var res = client.ListObjects(new ListObjectsRequest()
                                .WithBucketName(Context.BucketName)
                                .WithMaxKeys(maximumRows)
                                .WithMarker(q.Marker)
                                .WithPrefix(q.Prefix)))
                            {
                                return new IndexQueryResult()
                                {
                                    Results = res.S3Objects.Select(o => GetIndexEntryFromPath(o.Key)).ToList(),
                                    IsTruncated = res.IsTruncated,
                                };
                            }
                        }
                    });
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed querying index.", awsEx);
            }
        }

        private void PutPaths(IEnumerable<string> paths)
        {
            try
            {
                paths.AsParallel().ForAll(path =>
                    {
                        using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                        {
                            using (var res = client.PutObject(new PutObjectRequest()
                                .WithBucketName(Context.BucketName)
                                .WithKey(path))) { }
                        }
                    });
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed writing index entries.", awsEx);
            }
        }

        private void DeletePaths(IEnumerable<string> paths)
        {
            try
            {
                paths.AsParallel().ForAll(path =>
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        using (var res = client.DeleteObject(new DeleteObjectRequest()
                            .WithBucketName(Context.BucketName)
                            .WithKey(path))) { }
                    }
                });
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed deleting index entries.", awsEx);
            }
        }

        #endregion

        #region Static Helper Methods

        // TODO: Unit test index entry validation funciton.
        public static bool IsIndexEntryValid(IndexEntry indexEntry)
        {
            if (indexEntry == null || (indexEntry.SortHash == null | indexEntry.EntryText == null | indexEntry.EntryKey == Guid.Empty))
                return false;

            if (indexEntry.SortHash.Length > 41)
                return false;
            if (indexEntry.EntryText.Length > 60)
                return false;

            return true;
        }

        /// <summary>
        /// Expand an index entry inito it's associated index paths (the default path + tokenised paths).
        /// </summary>
        /// <param name="basePath">The base path under which the index will sit e.g. /a/cbcb3ea1251048b8b368d12db433bf9b/vi</param>
        /// <param name="indexEntry">Details of the index entry to create paths for.</param>
        /// <returns>A collection of paths.</returns>
        public static IEnumerable<string> GetIndexEntryPaths(IndexEntry indexEntry)
        {
            if (indexEntry == null)
                throw new ArgumentNullException("Index entry is null.");

            var indexEntryFilename = GetIndexEntryFilename(indexEntry);
            var paths = new List<string>(1 + ((indexEntry.Tokens != null) ? indexEntry.Tokens.Count() : 0));

            paths.Add(string.Format("{0}/e/{1}", indexEntry.BasePath, indexEntryFilename));

            if (indexEntry.Tokens != null && indexEntry.Tokens.Any())
                paths.AddRange(indexEntry.Tokens.Select(t => string.Format("{0}/t/{1}/{2}", indexEntry.BasePath, GetTokenHash(t), indexEntryFilename)));

            return paths;
        }

        public static string GetTokenHash(string token)
        {
            byte[] bytes;
            using (var algorithm = System.Security.Cryptography.MD5.Create())
            {
                bytes = algorithm.ComputeHash(Encoding.Default.GetBytes(token));
            }

            char[] c = new char[bytes.Length * 2];
            byte b;
            for (int y = 0, x = 0; y < bytes.Length; ++y, ++x)
            {
                b = ((byte)(bytes[y] >> 4));
                c[x] = (char)(b > 9 ? b + 0x57 : b + 0x30);
                b = ((byte)(bytes[y] & 0xF));
                c[++x] = (char)(b > 9 ? b + 0x57 : b + 0x30);
            }

            return new string(c);
        }

        public static string GetIndexEntryFilename(IndexEntry indexEntry)
        {
            return string.Format("{0}-{1}-{2}", indexEntry.SortHash, indexEntry.EntryKey.ToString("N"), System.Web.HttpUtility.UrlPathEncode(indexEntry.EntryText).Replace(".", "%2E"));
        }

        public static IndexEntry GetIndexEntryFromPath(string path)
        {
            if (!Regex.IsMatch(path, @"/\w+-\w{32}-(?:\w|%)+$"))
                throw new FormatException("Path is not a valid index entry.");

            int lastSlash = path.LastIndexOf("/");
            int firstDash = path.IndexOf('-', lastSlash + 1);
            int secondDash = path.IndexOf('-', firstDash + 1);

            string basePath = path.Substring(0, lastSlash - 1);
            string sortHash = path.Substring(lastSlash + 1, firstDash - (lastSlash + 1));
            string entryKeyText = path.Substring(firstDash + 1, secondDash - (firstDash + 1));
            string entryText = path.Substring(secondDash + 1);

            return new IndexEntry()
            {
                BasePath = basePath,
                SortHash = sortHash,
                EntryKey = new Guid(entryKeyText),
                EntryText = HttpUtility.UrlDecode(entryText),
            };
        }

        #endregion
    }
}
