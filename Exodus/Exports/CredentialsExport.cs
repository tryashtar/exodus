using AdysTech.CredentialManager;
using System.Text;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class CredentialsExport
{
    public readonly HashSet<string> Copy;
    public readonly HashSet<string> Delete;
    public readonly List<Credentials> Add;
    public void Finalize()
    {
        Console.WriteLine("Finalizing credentials...");
        foreach (var item in Copy)
        {
            var cm = CredentialManager.GetCredentials(item);
            if (cm != null)
                Add.Add(new Credentials(item, cm.UserName, Convert.ToBase64String(Encoding.Unicode.GetBytes(cm.Password))));
        }
        Copy.Clear();
    }
    public void Perform()
    {
        Console.WriteLine("Importing credentials...");
        foreach (var item in Add)
        {
            CredentialManager.SaveCredentials(item.Target, new System.Net.NetworkCredential(item.Username, Encoding.Unicode.GetString(Convert.FromBase64String(item.Password))), AllowNullPassword: true);
        }
        foreach (var item in Delete)
        {
            CredentialManager.RemoveCredentials(item);
        }
    }
}

public record Credentials(string Target, string Username, string Password)
{
    public Credentials() : this(default, default, default) { } // cringe
}
