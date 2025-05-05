using RepeatList.Models;
using Supabase;

namespace RepeatList.Services
{
    public class SupabaseService
    {
        private readonly Client _supabase;
        //private readonly DatabaseService _databaseService;

        public SupabaseService()
        {
            //_databaseService =  new DatabaseService();

            var supabaseKey = AppSettings.Load().Result.ApiKeys.SupabaseKey;
            _supabase = new Client(
                "https://bzjdutgysaztuszpcdlw.supabase.co",
                supabaseKey
                );
            _supabase.InitializeAsync().Wait();
        }

        public async Task SyncHeaderWithDetailsAsync(Header? header)
        {
            // Hole den Header aus der lokalen Datenbank
            //var header = await _databaseService.GetHeaderAsync(headerId);

            if (header != null)
            {
                await _supabase.From<Header>().Upsert(header);

                // Hole die zugehörigen Positions aus der lokalen Datenbank
                //var positions = await _databaseService.GetPositionsAsync(headerId);
                foreach (var position in header.Positions)
                {
                    await _supabase.From<Position>().Upsert(position);
                }
            }
        }

        public async Task SyncPositionAsync(Position? position)
        {
            await _supabase.From<Position>().Upsert(position);
        }
        

        public async Task DeleteHeaderWithDetailsAsync(Header header)
        {
            if (header != null)
            {
                //var positions = await _databaseService.GetPositionsAsync(header.Id);
                //foreach (var position in positions)
                //{
                //    await _supabase.From<Position>().Delete(position);
                //}
                await _supabase.From<Header>().Delete(header);
            }
        }

        public async Task DeletePositionAsync(Position position)
        {
            if (position != null)
            {
                await _supabase.From<Position>().Delete(position);
            }
        }

        public async Task<(Header header, List<Position> position)> GetHeaderWithPositionsByIdAsync(Guid headerId)
        {
            try
            {
                Supabase.Postgrest.Responses.ModeledResponse<Header> headerResponse = await _supabase
                    .From<Header>()
                    .Filter("Id", Supabase.Postgrest.Constants.Operator.Equals, headerId.ToString())
                    .Get();

                var _header = headerResponse.Model;

                if (headerResponse != null)
                {
                    // Hole die zugehörigen Details aus Supabase
                    var detailsResponse = await _supabase
                        .From<Position>()
                        .Filter("HeaderId", Supabase.Postgrest.Constants.Operator.Equals, headerId.ToString())
                        //.Where(x => x.HeaderId == headerId.ToString())
                        .Get();

                    var _position = detailsResponse.Models;

                    return (_header, _position);
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                if (ex != null)
                    SentrySdk.CaptureException(ex);

                return (null, null);
            }
        }
    }
}
