using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;

namespace Plywood.Internal
{
    public class Indexes : ControllerBase
    {
        public Indexes() : base() { }
        public Indexes(ControllerConfiguration context) : base(context) { }

        public void DeleteIndexEntry(string path, Guid key, bool throwNotFoundExceptions = false)
        {
            var index = LoadIndex(path);
            if (index.Entries.Any(e => e.Key == key))
            {
                index.Entries.Remove(index.Entries.Single(e => e.Key == key));
                Internal.Indexes.NameSortIndex(index);
                UpdateIndex(path, index);
            }
            else if (throwNotFoundExceptions)
            {
                throw new IndexEntryNotFoundException(string.Format("Could not find entry in index for key: {0}", key));
            }
        }

        public EntityIndex LoadIndex(string path)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObject(new GetObjectRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = path,
                    }))
                    {
                        var index = new EntityIndex() { ETag = res.ETag };
                        using (var stream = res.ResponseStream)
                        {
                            index.Entries = ParseIndex(stream);
                        }
                        return index;
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new EntityIndex() { Entries = new List<EntityIndexEntry>(), ETag = "" };
                }
                else
                {
                    throw new DeploymentException("Failed loading group index", awsEx);
                }
            }
        }

        public static void NameSortIndex(EntityIndex index, bool descending = false)
        {
            index.Entries.Sort(delegate(Internal.EntityIndexEntry a, Internal.EntityIndexEntry b)
            {
                int compare = string.Compare(a.Name, b.Name, true);
                if (descending)
                    return compare * -1;
                else
                    return compare;
            });
        }

        public void PutIndexEntry(string path, EntityIndexEntry entry, bool sortDescending = false)
        {
            bool modified;
            var index = LoadIndex(path);
            if (index.Entries.Any(e => e.Key == entry.Key))
            {
                var existingEntry = index.Entries.Single(e => e.Key == entry.Key);
                if (existingEntry.Name != entry.Name)
                {
                    existingEntry.Name = entry.Name;
                    modified = true;
                }
                else
                {
                    modified = false;
                }
            }
            else
            {
                index.Entries.Add(entry);
                modified = true;
            }
            if (modified)
            {
                Internal.Indexes.NameSortIndex(index, sortDescending);
                UpdateIndex(path, index);
            }
        }

        public void UpdateIndex(string path, EntityIndex index)
        {
            var serialised = SerialiseIndex(index.Entries);
            var stream = new MemoryStream(serialised.Length);
            var writer = new StreamWriter(stream);
            writer.Write(serialised);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    // Check that the index has not been updated before pushing the change - reduce the risks.
                    try
                    {
                        using (var objectMetaDataResponse = client.GetObjectMetadata(new GetObjectMetadataRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = path,
                            ETagToMatch = index.ETag
                        }))
                        {

                        }
                    }
                    catch (AmazonS3Exception awsEx)
                    {
                        if (awsEx.StatusCode != System.Net.HttpStatusCode.NotFound)
                            throw;
                    }

                    using (var putResponse = client.PutObject(new PutObjectRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = path,
                        GenerateMD5Digest = true,
                        InputStream = stream
                    })) { }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed loading group index", awsEx);
            }
        }

        public static string SerialiseIndex(List<EntityIndexEntry> entries)
        {
            if (entries.Any(e => !Utils.Validation.IsNameValid(e.Name)))
                throw new ArgumentException("Index entries contains invalid name");

            var sb = new StringBuilder();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    sb.AppendFormat("{0}\t{1}\r\n", entry.Key.ToString("N"), entry.Name);
                }
            }
            return sb.ToString();
        }

        public static List<EntityIndexEntry> ParseIndex(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream", "stream cannot be null.");

            var reader = new StreamReader(stream);
            return ParseIndex(reader);
        }

        public static List<EntityIndexEntry> ParseIndex(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader", "Reader cannot be null.");

            var entries = new List<EntityIndexEntry>();
            string currentLine = reader.ReadLine();
            while (currentLine != null)
            {
                entries.Add(ReadIndexEntry(currentLine));
                currentLine = reader.ReadLine();
            }
            return entries;
        }

        private static EntityIndexEntry ReadIndexEntry(string line)
        {
            if (String.IsNullOrWhiteSpace(line))
                throw new ArgumentException("Index entry malformed: Line is null or whitespace.", "line");
            if (line.Length < 32)
                throw new ArgumentException("Index entry malformed: line not long enough.", "line");

            string keyText = line.Substring(0, 32);
            Guid key;
            if (!Guid.TryParseExact(keyText, "N", out key))
                throw new ArgumentException("Index entry malformed: invalid key.", "line");

            string name = line.Substring(32, line.Length - 32).TrimStart('\t');

            return new EntityIndexEntry() { Name = name, Key = key };
        }

    }

    public class EntityIndex
    {
        public string ETag { get; set; }
        public List<EntityIndexEntry> Entries { get; set; }
    }

    public class EntityIndexEntry
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }

}
