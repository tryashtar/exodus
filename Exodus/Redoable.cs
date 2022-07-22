using TryashtarUtils.Utility;
using YamlDotNet.RepresentationModel;

namespace Exodus;

public static class Redoable
{
    public static void Do(Action action)
    {
        bool success = false;
        while (!success)
        {
            try
            {
                action();
                success = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("    Error: " + ex.ToString());
                Console.WriteLine("    Redo?");
                success = Console.ReadLine().ToLower() == "n";
            }
        }
    }
}

