using RepeatList.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepeatList.Services
{
    public class SupabaseService
    {
        private readonly Client _supabase;

        public SupabaseService()
        {
            _supabase = new Client("https://DEINE_SUPABASE_URL", "DEIN_SUPABASE_ANON_KEY");
            _supabase.InitializeAsync().Wait();
        }

        public async Task<List<Position>> GetItemsFromSupabaseAsync()
        {
            var response = await _supabase.From<Position>().Get();
            return response.Models;
        }

        public async Task AddItemToSupabaseAsync(Item item)
        {
            await _supabase.From<Position>().Insert(item);
        }
    }
}
