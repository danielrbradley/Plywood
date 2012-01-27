namespace Plywood
{
    /// <summary>
    /// Base class for all plywood framework controlers.
    /// </summary>
    public abstract class ControllerBase
    {
        /// <summary>
        /// Storage provider to be used for the lifetime of the controller.
        /// </summary>
        private IStorageProvider storageProvider;

        /// <summary>
        /// Initializes a new instance of the ControllerBase class with a storage provider.
        /// </summary>
        /// <param name="storageProvider">Storage provider for the controller to use.</param>
        public ControllerBase(IStorageProvider storageProvider)
        {
            this.storageProvider = storageProvider;
        }

        /// <summary>
        /// Gets the storage provider being used by the current instance of the controller.
        /// </summary>
        public IStorageProvider StorageProvider
        {
            get
            {
                return this.storageProvider;
            }
        }
    }
}