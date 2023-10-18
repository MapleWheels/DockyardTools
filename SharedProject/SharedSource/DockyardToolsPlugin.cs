using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Barotrauma;

namespace DockyardTools
{
    public partial class DockyardToolsPlugin : IAssemblyPlugin
    {
        public void Initialize()
        {
            // When your plugin is loading, use this instead of the constructor
            // Put any code here that does not rely on other plugins.
        }

        public void OnLoadCompleted()
        {
            // After all plugins have loaded
            // Put code that interacts with other plugins here.
        }

        public void PreInitPatching()
        {
            // Not yet supported: Called during the Barotrauma startup phase before vanilla content is loaded.
        }

        public void Dispose()
        {
            // Cleanup your plugin!
        }
    }
}
