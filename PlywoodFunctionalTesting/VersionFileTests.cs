using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood;
using System.Diagnostics;

namespace Plywood.Tests.Functional
{
    static class VersionFileTests
    {
        public static void Run(ControllerConfiguration context, Guid versionKey)
        {
            var versionsController = new Versions(context);

            System.IO.DirectoryInfo pushFolder = new System.IO.DirectoryInfo(Program.STR_PUSH_FOLDER);
            System.IO.DirectoryInfo pullFolder = new System.IO.DirectoryInfo(Program.STR_PULL_FOLDER);
            versionsController.PushVersion(pushFolder, versionKey);

            if (!pullFolder.Exists)
            {
                pullFolder.Create();
                pullFolder.Refresh();
            }

            versionsController.PullVersion(versionKey, pullFolder);

            pullFolder.Refresh();
            var pulledFiles = pullFolder.GetFiles("*", System.IO.SearchOption.AllDirectories);
            Debug.Assert(pulledFiles.Length == 2);
            Debug.Assert(pulledFiles.Any(f => f.FullName == String.Format(@"{0}\Subfolder\AnImage.bmp", Program.STR_PULL_FOLDER)));
            Debug.Assert(pulledFiles.Any(f => f.FullName == String.Format(@"{0}\README.txt", Program.STR_PULL_FOLDER)));
        }
    }
}
