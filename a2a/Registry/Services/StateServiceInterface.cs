using Registry.Models;
namespace Registry.Services;
public interface IDbService
{
    void InsertIn(Servers server);
    void RemoveFrom(String uri);

    List<Uri> GetAllUris();
}
