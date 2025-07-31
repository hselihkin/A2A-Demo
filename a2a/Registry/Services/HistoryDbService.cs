using Registry.Data;
using Registry.Models;
namespace Registry.Services
{
    public class HistoryDbService(ApplicationDbContext context) : IHistoryService
    {
        public ApplicationDbContext _context = context;

        public void InsertIn(History history)
        {
            _context.History.Add(history);
            _context.SaveChanges();
        }

        public void UpdateIn(Uri uri)
        {
            var history = _context.History.FirstOrDefault(h => h.Uri == uri);
            if (history != null)
            {
                history.LeaveTime = DateTime.Now;
                _context.SaveChanges();
            }
        }

    }
}
