# Notizen / Entwicklungs-Log

Technische Notizen zu Bugs, Fixes und offenen Punkten, die über einzelne Commits hinaus relevant bleiben. Git-versioniert, damit sie bei Entwicklung an verschiedenen Rechnern mitwandern.

## Background-ANR (behoben 14.08.2026)

**Sentry #139875795.** Ursache: Main-Thread-Blocking durch fake-async `Microsoft.Data.Sqlite` (führt synchron aus) + Sync-Schleife in `ListsPage.OnAppearing` (`ForTimer_Tick` → `Sync_list_downClicked`), während die App backgrounded wurde.

**Fixes:**
- `DatabaseService` auf Thread-Pool verlagert (lazy Connect, `SemaphoreSlim`-Serialisierung, `RunExclusiveAsync`-Wrapper); Konstruktor blockiert nicht mehr.
- Race `_ = setupPageViewModel.Load()` vs. `SelectedItem` behoben (Culture-Load in `InitializeCultureAsync`).
- Reentrancy-Guard (`Interlocked.Exchange` / `SemaphoreSlim`) gegen stapelnde Syncs.
- CancellationToken-Kette (`_syncCts`); Sync bricht bei `AppLifecycle.Backgrounded` / `OnDisappearing` ab.
- `CollectionView` auf `MeasureFirstItem`; Sentry `Native.AnrEnabled` + `AnrTimeoutInterval=3s`, `Debug` nur in DEBUG.

## Sentry-Symbol-Upload (seit 05.09.2026)

Eigenes MSBuild-Target `UploadSymbolsToSentry` in `RepeatList.csproj` (`AfterTargets="Build"`, nur Release/Android). Grund: Die offizielle Sentry-MSBuild-Integration (`SentryUploadSymbols`/`UploadDebugInfoToSentry`) scheitert auf der EU-Instanz `de.sentry.io`, weil der Auth-Check `GET /api/0/` dort 404 liefert und `sentry-cli` daraufhin den CLI-Pfad leert. Das eigene Target sammelt PDBs + native `.so`-ELF-Symbole und lädt sie direkt nach jedem Release-Build hoch.

Auth-Token liegt in `~/.sentryclirc` bzw. `SENTRY_AUTH_TOKEN` — **nicht** im Repo.

## Offener Verdachtsfall: Mono-Loader-Lock-ANR (Version 1.0.157, gemeldet 07.09.2026)

Sentry meldet erneut `ApplicationNotResponding: Background ANR`, Stack zeigt `mono_loader_lock` / `mono_metadata_get_generic_inst` (Class-/Generic-Loading-Deadlock im Mono-Loader), keine App-Frames sichtbar.

- Build 1.0.157 entstand 31.08.–02.09.2026 — **nach** dem obigen ANR-Fix; alle damaligen Fixes sind im Code enthalten und griffen hier offenbar nicht.
- Build 157 liegt **vor** dem Symbol-Upload-Target (05.09.), daher fehlen Debug-Symbole — der Stack ist nicht auf App-Code zurückführbar.
- Vermutung: anderes Problem als der ursprüngliche SQLite/Sync-Bug, evtl. Mono-Runtime/AOT-seitig statt App-Logik.
- **Nächster Schritt:** Nächsten ANR mit einem Build ≥ 1.0.162 (nach Symbol-Upload-Integration) neu bewerten — der Stack sollte dann App-Frames enthalten.
