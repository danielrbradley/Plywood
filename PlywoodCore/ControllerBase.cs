using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Plywood
{
    public abstract class ControllerBase
    {
        private IStorageProvider storageProvider;
        public IStorageProvider StorageProvider
        {
            get
            {
                return storageProvider;
            }
        }

        public ControllerBase(IStorageProvider storageProvider)
        {
            this.storageProvider = storageProvider;
        }
    }
}