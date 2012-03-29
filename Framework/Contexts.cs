using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Indexes;
using Plywood.Utils;
using System.IO;

namespace Plywood
{
    public class Contexts : ControllerBase
    {
        public Contexts(IStorageProvider provider)
            : base(provider)
        {
        }

        public void Create(Context context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context", "Context cannot be null.");
            }

            using (var stream = context.Serialise())
            {
                try
                {
                    var indexEntries = new IndexEntries(StorageProvider);

                    StorageProvider.PutFile(context.GetDetailsPath(), stream);
                    indexEntries.PutEntity(context);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating group.", ex);
                }
            }
        }

        public void Delete(Guid key)
        {
            var context = this.Get(key);
            if (context == null)
                throw new ContextNotFoundException(string.Format("A context with the key \"{0}\" could not be found to delete."));

            if ((new Packages(this.StorageProvider)).Search(key, pageSize: 0).IsTruncated)
                throw new DeploymentException("Context to delete still has packages assigned to its self.");

            if ((new Roles(this.StorageProvider)).Search(key, pageSize: 0).IsTruncated)
                throw new DeploymentException("Context to delete still has roles assigned to its self.");

            try
            {
                var indexEntries = new IndexEntries(StorageProvider);
                indexEntries.DeleteEntity(context);

                var detailsPath = context.GetDetailsPath();
                StorageProvider.MoveFile(detailsPath, string.Concat("deleted/", detailsPath));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting context.", ex);
            }
        }

        public bool Exists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Context.GetDetailsPath(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting context with key \"{0}\"", key), ex);
            }
        }

        public Context Get(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Context.GetDetailsPath(key)))
                {
                    return new Context(stream);
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public ContextList Search(string context = null, bool recursive = false, string query = null, string marker = null, int pageSize = 50)
        {
            throw new NotImplementedException();
        }
    }
}
