namespace Plywood
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Plywood.Indexes;
    using Plywood.Utils;

    /// <summary>
    /// Provides methods to interact with groups.
    /// </summary>
    public class Groups : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the Groups class with the given storage provider.
        /// </summary>
        /// <param name="provider">Provider the controller will use to access the storage platform.</param>
        public Groups(IStorageProvider provider)
            : base(provider)
        {
        }

        /// <summary>
        /// Creates a new group.
        /// </summary>
        /// <param name="group">Details of the group to create.</param>
        public void Create(Group group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group", "Group cannot be null.");
            }

            using (var stream = group.Serialise())
            {
                try
                {
                    var indexEntries = new IndexEntries(StorageProvider);

                    StorageProvider.PutFile(Paths.GetGroupDetailsKey(group.Key), stream);
                    indexEntries.PutEntity(group);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating group.", ex);
                }
            }
        }

        /// <summary>
        /// Deletes an existing group.
        /// </summary>
        /// <param name="key">Identifier of the group to delete.</param>
        public void Delete(Guid key)
        {
            var group = this.Get(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);
                indexEntries.DeleteEntity(group);

                // TODO: Refactor the self-delete functionality.
                StorageProvider.MoveFile(Paths.GetGroupDetailsKey(key), string.Concat("deleted/", Paths.GetGroupDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting group.", ex);
            }
        }

        /// <summary>
        /// Checks if a group with a given key exists.
        /// </summary>
        /// <param name="key">Identifier of the group to check existance of.</param>
        /// <returns>true if a group with the key exists; otherwise false.</returns>
        public bool Exists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetGroupDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting group with key \"{0}\"", key), ex);
            }
        }

        /// <summary>
        /// Gets a group.
        /// </summary>
        /// <param name="key">Identifier of the group to get.</param>
        /// <returns>A group object or null if not found.</returns>
        public Group Get(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetGroupDetailsKey(key)))
                {
                    return new Group(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting group with key \"{0}\"", key), ex);
            }
        }

        /// <summary>
        /// Search groups and return a page of results.
        /// </summary>
        /// <param name="query">Set of terms (case insensitive) to query against the group names.</param>
        /// <param name="marker">Marker indicating the offset to take results logically after (null for first page).</param>
        /// <param name="pageSize">Maximum number of results to return in the returned result set (0 - 100)</param>
        /// <returns>A list of groups matching the criteria.</returns>
        public GroupList Search(string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 0)
            {
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 0.");
            }

            if (pageSize > 100)
            {
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");
            }

            try
            {
                IEnumerable<string> basePaths;

                bool queryIsSpecified = !string.IsNullOrWhiteSpace(query);
                if (queryIsSpecified)
                {
                    basePaths = new SimpleTokeniser().Tokenise(query).Select(token =>
                        string.Format("gi/t/{0}", Indexes.IndexEntries.GetTokenHash(token)));
                }
                else
                {
                    basePaths = new List<string>()
                    {
                        "gi/e",
                    };
                }

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                IEnumerable<GroupListItem> groups = rawResults.FileNames.Select(fileName => new GroupListItem(fileName));
                var list = new GroupList()
                {
                    Groups = groups,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = groups.Any() ? groups.Last().Marker : marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searching groups.", ex);
            }
        }

        /// <summary>
        /// Update the details of an existing group.
        /// </summary>
        /// <param name="group">Updated details of the group.</param>
        public void Update(Group group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group", "group is null.");
            }

            var oldGroup = this.Get(group.Key);

            using (var stream = group.Serialise())
            {
                try
                {
                    StorageProvider.PutFile(Paths.GetGroupDetailsKey(group.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.UpdateEntity(oldGroup, group);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating group.", ex);
                }
            }
        }
    }
}