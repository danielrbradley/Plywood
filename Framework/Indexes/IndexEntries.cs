using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace Plywood.Indexes
{
    public class IndexEntries : ControllerBase
    {
        public IndexEntries(IStorageProvider provider) : base(provider) { }

        public void DeleteEntity(IIndexableEntity entity)
        {
            var paths = entity.GetIndexEntries();
            DeletePaths(paths);
        }

        public void PutEntity(IIndexableEntity entity)
        {
            var paths = entity.GetIndexEntries();
            PutPaths(paths);
        }

        public void UpdateEntity(IIndexableEntity oldEntity, IIndexableEntity newEntity)
        {
            var oldPaths = oldEntity.GetIndexEntries();
            var newPaths = newEntity.GetIndexEntries();

            var deletes = oldPaths.Where(p => !newPaths.Contains(p));
            var puts = newPaths.Where(p => !oldPaths.Contains(p));

            PutPaths(puts);
            DeletePaths(deletes);
        }

        public RawQueryResult PerformRawQuery(int maximumRows, string marker, IEnumerable<string> basePaths)
        {
            var mapJob = basePaths.AsParallel().Select(q =>
            {
                var rows = StorageProvider.ListFiles(q, marker, maximumRows);
                return new RawQueryResult()
                {
                    FileNames = rows.Items,
                    IsTruncated = rows.IsTruncated,
                };
            }).ToList();

            return new RawQueryResult()
            {
                FileNames = mapJob.SelectMany(j => j.FileNames).OrderBy(r => r).Distinct().Take(maximumRows).ToList(),
                IsTruncated = mapJob.Any(j => j.IsTruncated),
            };
        }

        #region Internal Methods
        // We could make these internal methods asynchronous to improve performance on updates.

        private void PutPaths(IEnumerable<string> paths)
        {
            try
            {
                paths.AsParallel().ForAll(path =>
                    {
                        StorageProvider.PutFile(path);
                    });
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed writing index entries.", ex);
            }
        }

        private void DeletePaths(IEnumerable<string> paths)
        {
            try
            {
                paths.AsParallel().ForAll(path =>
                {
                    try
                    {
                        StorageProvider.DeleteFile(path);
                    }
                    catch (FileNotFoundException)
                    {
                        // Ignore missing index entries.
                    }
                });
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting index entries.", ex);
            }
        }

        #endregion

        #region Static Helper Methods

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

        #endregion
    }
}
