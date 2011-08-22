using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public interface IIndexableEntity
    {
        IEnumerable<string> GetIndexEntries();
    }
}
