namespace ReturnNotFound.Client;

// Add properties to this class and update the server and client AuthenticationStateProviders
// to expose more information about the authenticated user to the client.
public class UserInfo
{
    public string Email { get; set; }

    public string UserId { get; set; }
}