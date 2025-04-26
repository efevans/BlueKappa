using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Yaush.Client.Pages
{
    [SupportedOSPlatform("browser")]
    public partial class Home
    {
        [JSImport("toClipboard", "Home")]
        internal static partial void ToClipboard(string text);
    }
}
