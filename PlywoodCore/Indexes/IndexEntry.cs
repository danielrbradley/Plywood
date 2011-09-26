using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Indexes
{
    public class IndexEntry
    {
        public IndexEntry()
        {
        }

        public IndexEntry(string path)
        {
            var temp = IndexEntries.GetIndexEntryFromPath(path);
            this.SortHash = temp.SortHash;
            this.EntryKey = temp.EntryKey;
            this.EntryText = temp.EntryText;
        }

        public string BasePath { get; set; }
        public string SortHash { get; set; }
        public Guid EntryKey { get; set; }
        public string EntryText { get; set; }
        public IEnumerable<string> Tokens { get; set; }

        public override bool Equals(object obj)
        {
            if (obj ==  null)
                return false;
            if (obj.GetType() != typeof(IndexEntry))
                return false;
            return this.Equals(obj as IndexEntry);
        }

        public bool Equals(IndexEntry indexEntry)
        {
            if (indexEntry == null)
                return false;
            return this.EntryKey.Equals(indexEntry.EntryKey);
        }

        public override int GetHashCode()
        {
            return this.EntryKey.GetHashCode();
        }

        public override string ToString()
        {
            return IndexEntries.GetIndexEntryFilename(this);
        }

        public bool IsValid()
        {
            return IndexEntries.IsIndexEntryValid(this);
        }

        public IEnumerable<string> GetPaths()
        {
            return IndexEntries.GetIndexEntryPaths(this);
        }
    }
}
