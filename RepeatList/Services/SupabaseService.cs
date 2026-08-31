using RepeatList.Models;
using Supabase;
using System.Threading;
using static AndroidX.ConstraintLayout.Core.Motion.Utils.HyperSpline;

namespace RepeatList.Services
{
    public class SupabaseService
    {
        /// <summary>Shared instance so all callers reuse one client and one initialization.</summary>
        public static SupabaseService Shared { get; } = new();

        private static readonly TimeSpan InitializeTimeout = TimeSpan.FromSeconds(15);

        private readonly Client _supabase;
        private readonly object _initLock = new();
        private Task? _initializationTask;
        private bool _initialized;

        public SupabaseService()
        {
            var supabaseKey = SecretVault.SupabaseKey;
            _supabase = new Client(
                "https://bzjdutgysaztuszpcdlw.supabase.co",
                supabaseKey
                );
            // No blocking network call in the constructor:
            // initialization runs lazily on first use (see EnsureInitializedAsync).
        }

        private async Task EnsureInitializedAsync()
        {
            if (Volatile.Read(ref _initialized)) return;

            Task task;
            lock (_initLock)
            {
                if (Volatile.Read(ref _initialized)) return;
                task = _initializationTask!;
                if (task == null)
                {
                    task = _initializationTask = InitializeCoreAsync();
                    // Fehler beobachten + Cache zurücksetzen → der nächste Aufruf versucht neu.
                    // (Die Task gilt erst als "erledigt", wenn der Client wirklich bereit ist.)
                    _ = task.ContinueWith(t =>
                    {
                        _ = t.Exception; // als beobachtet markieren (kein UnobservedTaskException)
                        lock (_initLock)
                        {
                            if (ReferenceEquals(_initializationTask, t))
                                _initializationTask = null;
                        }
                    }, CancellationToken.None,
                       TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
                       TaskScheduler.Default);
                }
            }

            // Pro Aufruf gedeckelt: Der Aufrufer (z. B. der 15-s-Timer) hängt nie länger als 15 s.
            // Läuft die echte Init länger, bleibt die Task aktiv; der nächste Tick wartet erneut.
            using var timeoutCts = new CancellationTokenSource(InitializeTimeout);
            Task completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, timeoutCts.Token)).ConfigureAwait(false);

            if (completed != task)
            {
                // Noch nicht bereit — KEIN dauerhafter Fehlerzustand: kein stale cache, der nächste
                // Aufruf wartet auf dieselbe (noch laufende) Task und versucht es dann erneut.
                SentrySdk.CaptureMessage($"SupabaseService init timed out after {InitializeTimeout.TotalSeconds}s");
                return;
            }

            await task; // rethrow on failure → Fault-Continuation setzt den Cache zurück
        }

        private async Task InitializeCoreAsync()
        {
            await _supabase.InitializeAsync().ConfigureAwait(false);
            Volatile.Write(ref _initialized, true);
        }

        /// <summary>Anzahl der Versuche inkl. Erstversuch bei transienten Netzwerkfehlern
        /// (z. B. "Software caused connection abort" bei Netzwechsel oder Keep-alive-Abbruch).</summary>
        private const int MaxAttempts = 3;
        private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMilliseconds(500);

        // Deckelt jeden einzelnen Supabase-Request. Ohne dies klemmt der Sync beim Appstart bis zum
        // HttpClient-Default (~100 s) × Retries, wenn das Netzwerk hängt ("App friert beim Sync").
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

        private static bool IsTransientNetworkError(Exception ex)
        {
            Exception baseEx = ex.GetBaseException();

            // HTTP-Statusfehler: nur 5xx ist transient (Server kurzzeitig überlastet)
            if (baseEx is System.Net.Http.HttpRequestException httpEx && httpEx.StatusCode.HasValue)
                return (int)httpEx.StatusCode.Value >= 500;

            // Alte WebException-Klasse: Protokoll-/TLS-Fehler nicht wiederholen
            if (baseEx is System.Net.WebException webEx)
                return webEx.Status != System.Net.WebExceptionStatus.ProtocolError
                    && webEx.Status != System.Net.WebExceptionStatus.SecureChannelFailure;

            // Verbindungsfehler (Socket-Abbruch, Keep-alive-Reuse) und Request-Timeout sind transient
            return baseEx is System.Net.Http.HttpRequestException
                or System.Net.Sockets.SocketException
                or Java.Net.SocketException
                or TimeoutException;
        }

        // Führt einen Supabase-Request mit festem Timeout aus. Bei Timeout wird TimeoutException
        // geworfen → von ExecuteWithRetryAsync als transient behandelt (Retry mit Backoff).
        private static async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation)
        {
            using var timeoutCts = new CancellationTokenSource(RequestTimeout);
            Task<T> task = operation();
            Task completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, timeoutCts.Token));
            if (completed != task)
                throw new TimeoutException($"Supabase request timed out after {RequestTimeout.TotalSeconds}s");
            return await task;
        }

        private static async Task ExecuteWithTimeoutAsync(Func<Task> operation)
        {
            using var timeoutCts = new CancellationTokenSource(RequestTimeout);
            Task task = operation();
            Task completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, timeoutCts.Token));
            if (completed != task)
                throw new TimeoutException($"Supabase request timed out after {RequestTimeout.TotalSeconds}s");
            await task;
        }

        private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return await ExecuteWithTimeoutAsync(operation);
                }
                catch (Exception ex) when (attempt < MaxAttempts && IsTransientNetworkError(ex))
                {
                    await Task.Delay(RetryBaseDelay * attempt);
                }
            }
        }

        private static async Task ExecuteWithRetryAsync(Func<Task> operation)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    await ExecuteWithTimeoutAsync(operation);
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts && IsTransientNetworkError(ex))
                {
                    await Task.Delay(RetryBaseDelay * attempt);
                }
            }
        }

        public async Task<bool> SyncHeaderWithDetailsAsync(Header? header)
        {
            if (header == null)
                return false;

            await EnsureInitializedAsync();

            try
            {
                await ExecuteWithRetryAsync(() => _supabase.From<Header>().Upsert(header));

                if (header.Positions != null)
                {
                    foreach (var position in header.Positions.Where(p => p != null))
                    {
                        await ExecuteWithRetryAsync(() => _supabase.From<Position>().Upsert(position));
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Nach Retries noch fehlgeschlagen → einmal melden; Aufrufer läuft lokal weiter
                SentrySdk.CaptureException(ex);
                return false;
            }
        }

        public async Task<bool> SyncPositionAsync(Position? position)
        {
            await EnsureInitializedAsync();

            try
            {
                await ExecuteWithRetryAsync(() => _supabase.From<Position>().Upsert(position));
                return true;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                return false;
            }
        }


        public async Task<bool> DeleteHeaderWithDetailsAsync(Header header)
        {
            await EnsureInitializedAsync();

            if (header == null)
                return false;

            try
            {
                //var positions = await _databaseService.GetPositionsAsync(header.Id);
                //foreach (var position in positions)
                //{
                //    await _supabase.From<Position>().Delete(position);
                //}
                await ExecuteWithRetryAsync(() => _supabase.From<Header>().Delete(header));
                return true;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                return false;
            }
        }

        public async Task<bool> DeletePositionAsync(Position position)
        {
            await EnsureInitializedAsync();

            if (position == null)
                return false;

            try
            {
                await ExecuteWithRetryAsync(() => _supabase.From<Position>().Delete(position));
                return true;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                return false;
            }
        }

        public async Task<(Header? header, List<Position>? position)> GetHeaderWithPositionsByIdAsync(Guid headerId)
        {
            try
            {
                await EnsureInitializedAsync();

                Supabase.Postgrest.Responses.ModeledResponse<Header> headerResponse = await ExecuteWithRetryAsync(
                    () => _supabase
                        .From<Header>()
                        .Filter("Id", Supabase.Postgrest.Constants.Operator.Equals, headerId.ToString())
                        .Get());

                var _header = headerResponse.Model;

                if (headerResponse != null)
                {
                    // Hole die zugehörigen Details aus Supabase
                    var detailsResponse = await ExecuteWithRetryAsync(
                        () => _supabase
                            .From<Position>()
                            .Filter("HeaderId", Supabase.Postgrest.Constants.Operator.Equals, headerId.ToString())
                            //.Where(x => x.HeaderId == headerId.ToString())
                            .Get());

                    var _position = detailsResponse.Models;

                    return (_header, _position);
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                if (ex != null)
                    SentrySdk.CaptureException(ex, scope => scope.SetTag("sync.direction", "down"));

                return (null, null);
            }
        }

        public async Task UpsertSubscriptionAsync(string deviceId, bool isPremium, string? purchaseToken = null, string? productId = null)
        {
            try
            {
                await EnsureInitializedAsync();

                var sub = new Subscription
                {
                    DeviceId = deviceId,
                    IsPremium = isPremium,
                    PurchaseToken = purchaseToken,
                    ProductId = productId,
                    UpdatedAt = DateTime.UtcNow
                };
                await ExecuteWithRetryAsync(() => _supabase.From<Subscription>().Upsert(sub));
            }
            catch (Exception)
            {
                // Offline oder Fehler — lokaler Cache bleibt gültig
            }
        }

        public async Task<bool?> GetSubscriptionStatusAsync(string deviceId)
        {
            try
            {
                await EnsureInitializedAsync();

                var response = await ExecuteWithRetryAsync(
                    () => _supabase
                        .From<Subscription>()
                        .Filter("device_id", Supabase.Postgrest.Constants.Operator.Equals, deviceId)
                        .Single());
                return response?.IsPremium;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<DeviceList>?> GetDeviceListAsync()
        {
            try
            {
                await EnsureInitializedAsync();

                Supabase.Postgrest.Responses.ModeledResponse<DeviceList> headerResponse = await ExecuteWithRetryAsync(
                    () => _supabase
                        .From<DeviceList>()
                        //.Filter("Id", Supabase.Postgrest.Constants.Operator.Equals, headerId.ToString())
                        .Get());

                var _header = headerResponse.Models;

                if (_header != null)
                {
                    return (_header);
                }
                return (null);
            }
            catch (Exception ex)
            {
                //if (ex != null)
                //    SentrySdk.CaptureException(ex);

                return (null);
            }
        }
    }
}
