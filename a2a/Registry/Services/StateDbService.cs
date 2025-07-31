using Registry.Data;
using Registry.Models;
namespace Registry.Services
{
    public class StateDbService(ApplicationDbContext context) : IDbService
    {
        public ApplicationDbContext _context = context;

        public void InsertIn(Servers server)
        {
            _context.Servers.Add(server);
            _context.SaveChanges();
        }

        public void RemoveFrom(String uri)
        {
            Console.WriteLine(uri);
            var server = _context.Servers.Find(uri);
            if (server == null)
            {
                Console.WriteLine("why bro");
            }
            Console.WriteLine(server);
            if (server != null)
            {
                _context.Servers.Remove(server);
                _context.SaveChanges();
            }
        }

        public List<Uri> GetAllUris()
        {
            var server = _context.Servers.ToList();
            var uriList = new List<Uri>();
            if (server.Count != 0)
            {

                foreach (var uri in server)
                {
                    uriList.Add(new Uri(uri.Uri));
                }
            }
            return uriList;

        }
    }
}
