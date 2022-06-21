using AdysTech.CredentialManager;

namespace Exodus;

[YamlParser.OptionalFields(true)]
public class CredentialsExport
{
    public readonly HashSet<string> Copy;
    public readonly HashSet<string> Delete;
    public readonly List<Credentials> Add;
    public void Finalize()
    {
        foreach (var item in Copy)
        {
            var cm = CredentialManager.GetCredentials(item);
            if (cm != null)
                Add.Add(new Credentials(item, cm.UserName, cm.Password));
        }
        Copy.Clear();
    }
    public void Perform()
    {
        foreach (var item in Add)
        {
            CredentialManager.SaveCredentials(item.Target, new System.Net.NetworkCredential(item.Username, item.Password));
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
