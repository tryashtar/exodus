using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exodus;

public static class WingetWrapper
{
    public static bool IsInstalled(string name)
    {
        var cmd = ProcessWrapper.RunCommand("winget", $"list --exact \"{name}\"");
        return cmd.ExitCode == 0;
    }
}
