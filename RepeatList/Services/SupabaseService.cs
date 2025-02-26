using RepeatList.Models;
using Supabase;

namespace RepeatList.Services
{
    public class SupabaseService
    {
        private readonly Client _supabase;
        private readonly DatabaseService _databaseService;

        public SupabaseService()
        {
            _databaseService =  new DatabaseService();

            _supabase = new Client(
                "https://bzjdutgysaztuszpcdlw.supabase.co",
                //"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJ6amR1dGd5c2F6dHVzenBjZGx3Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0MDE0Nzc1OSwiZXhwIjoyMDU1NzIzNzU5fQ.GGoBd_7eDfDuRD7Z7jfMhJFGbYj107DtRIxZGK0UsBM"
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJ6amR1dGd5c2F6dHVzenBjZGx3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDAxNDc3NTksImV4cCI6MjA1NTcyMzc1OX0.C4nfCqnKbAu9G35B3u_ljDG7qj_JVkKRhblsAWQ8eXc"
                );
            _supabase.InitializeAsync().Wait();

           var session = _supabase.Auth.SignIn("dnepr65@gmail.com", "Haas1946");

            //if (session.use != null)
            //{
            //    Console.WriteLine($"Benutzer-ID: {session.User.Id}");
            //}
            //else
            //{
            //    Console.WriteLine("Fehler: Anmeldung fehlgeschlagen!");
            //}

            //var user = _supabase.Auth.CurrentUser?.Id;

        }

        public async Task SyncHeaderWithDetailsAsync(string headerId)
        {
            // Hole den Header aus der lokalen Datenbank
            var header = await _databaseService.GetHeaderAsync(headerId);

            if (header != null)
            {
                await _supabase.From<Header>().Upsert(header);

                // Hole die zugehörigen Positions aus der lokalen Datenbank
                var positions = await _databaseService.GetPositionsAsync(headerId);
                foreach (var position in positions)
                {
                    await _supabase.From<Position>().Upsert(position);
                }
            }
        }

        public async Task<(Header Header, List<Position> Details)> GetHeaderWithPositionsByIdAsync(Guid headerId)
        {
           // Hole den Header aus Supabase
           var headerResponse = await _supabase
               .From<Header>()
               .Where(x => x.Id == headerId.ToString())
               .Single();

            //var header = headerResponse.Model;

            if (headerResponse != null)
            {
                // Hole die zugehörigen Details aus Supabase
                var detailsResponse = await _supabase
                    .From<Position>()
                    .Where(x => x.HeaderId == headerId.ToString())
                    .Get();

                var details = detailsResponse.Models;

                return (headerResponse, details);
            }

            return (null, null);
        }
    }
}
