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

            var supabaseKey =  AppSettings.Load().Result.ApiKeys.SupabaseKey; 
            _supabase = new Client(
                "https://bzjdutgysaztuszpcdlw.supabase.co",
                supabaseKey
                );
            _supabase.InitializeAsync().Wait();
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
