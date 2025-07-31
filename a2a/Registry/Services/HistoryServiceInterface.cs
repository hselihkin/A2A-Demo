using Registry.Models;
namespace Registry.Services;
public interface IHistoryService
{
    void InsertIn(History history);
    void UpdateIn(Uri uri);
}
