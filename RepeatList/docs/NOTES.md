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
- **Zweiter Auftritt (Version 1.0.159, gemeldet 08.09.2026):** identischer Stack (`mono_loader_lock`/`mono_metadata_get_generic_inst`, keine App-Frames, nur `main`-Thread) — bestätigt, dass es kein Einzelfall war. Liegt weiterhin vor 1.0.162, also vor der geplanten Neubewertung.
- **Dritter Auftritt (Version 1.0.169, Android 15, Sentry DOTNET-MAUI-4D, ausgewertet 08.10.2026):** wieder `mono_loader_lock` auf `main`, weiterhin ohne App-Frames — die Erwartung „ab 1.0.162 sind App-Frames sichtbar" hat sich nicht erfüllt.
  - Die von Sentry als `libbinder` beschrifteten Frames gehören zu `libmonosgen-2.0.so` (Runtime-Pack 9.0.20, Build-ID `6bceca0e711d8fb06f6ba715055d597afce5c4b6`). Von Hand aufgelöst: `mono_magic_trampoline` → `common_call_trampoline` → `mono_jit_compile_method` → `mono_jit_compile_method_with_opt` → `mono_jit_compile_method_inner` → `mono_loader_lock`.
  - Bedeutung: Der Main-Thread ruft eine noch nicht kompilierte Methode auf, will sie JIT-kompilieren und wartet auf die Loader-Sperre, die ein anderer Thread hält. Welche Methode und welcher Thread, ist aus dem `main`-Stack allein nicht erkennbar.
  - Symbole für `libmonosgen-2.0.so` liegen nicht im Build-Output und werden vom Target `UploadSymbolsToSentry` daher nicht hochgeladen. Quelle: `https://msdl.microsoft.com/download/symbols/_.debug/elf-buildid-sym-<Build-ID>/_.debug`.
- **Halter gefunden (08.10.2026, gleiches Event): Deadlock innerhalb der Mono-Runtime, kein App-Code beteiligt.**
  - `.NET TP Worker` #9253 hält die Loader-Sperre und ist suspendiert: `mono_jit_compile_method_inner` → `mini_method_compile` → `mono_method_to_ir` → `mono_class_vtable_checked` (nimmt die Sperre) → `mono_gc_get_vtable_bits` → `class_kind` → `gc_bridge_class_kind_cb` (libmono-android) → `mono_class_is_subclass_of` → `mono_threads_enter_gc_unsafe_region_unbalanced_with_info` → `mono_threads_state_poll_with_info` → `mono_thread_info_wait_for_resume` (`sem_wait`).
  - `Finalizer` #9163 hat ihn suspendiert und will danach selbst die Sperre: `finalizer_thread` → `mono_threads_perform_thread_dump` → `mono_thread_info_safe_suspend_and_run` → `get_thread_dump` → `mono_walk_stack_with_state` → … → `mono_aot_find_jit_info` → `mono_get_method_checked` → `mono_class_create_from_typedef` → `mono_loader_lock`.
  - Folge: #9253 wird nie fortgesetzt, die Sperre nie frei. `main`, `.NET Timer`, `Thread-11` und zwei weitere TP-Worker hängen dahinter.
  - Auslöser des Thread-Dumps ist nicht belegt; vermutet wird ein `SIGQUIT` von Android (Stack-Dump-Anforderung), auf den Mono mit einem eigenen Thread-Dump reagiert.
  - Konsequenz: `AnrTimeoutInterval` ändert daran nichts (echter Deadlock, keine Verzögerung). Websuche nach einem passenden Upstream-Issue am 08.10.2026 ohne Treffer.
- **Nächster Schritt:** Entscheiden, ob der Fall bei dotnet/runtime gemeldet wird und ob ein Wechsel auf .NET 10 geprüft wird. In Sentry nicht als behoben abhaken.

## In-App-Updates (Flexible Flow, seit 07.09.2026)

Nutzer hingen bisher komplett am Play-Store-Auto-Update — es gab **nie** eine echte In-App-Update-Funktion in der App (`Platforms\Android\InAppUpdateManager.cs` existierte seit 02.05.2025 nur als leere, unbenutzte Klassenhülle und wurde einen Tag später sogar aus dem Build ausgeschlossen).

**Umgesetzt:** Google Play Core "Flexible" In-App-Update (`Platforms\Android\InAppUpdateManager.cs`, aufgerufen aus `MainActivity.OnResume`):
- Lädt eine verfügbare neue Version im Hintergrund herunter, ohne die App zu blockieren.
- Nach Abschluss (oder beim nächsten `OnResume`, falls der Download bereits fertig war) fragt ein `DisplayAlert` den Nutzer, ob er jetzt neu starten möchte (`AppUpdateManager.CompleteUpdate()`).
- Die nativen Play-Core-Maven-Libraries (`com.google.android.play:app-update:2.1.0`, `core-common:2.0.3`) waren im csproj schon länger referenziert, aber nie genutzt.

**Bewusst weggelassen:** Ein `InstallStateUpdatedListener` (würde sofort benachrichtigen, sobald der Download fertig ist, auch während die App offen ist) — die generische Java-Signatur (`StateUpdatedListener<InstallState>`) lässt sich mit den aktuellen Xamarin/MAUI-Play-Core-Bindings nicht sauber implementieren (javac-Fehler: `onStateUpdate(Object)` vs. `onStateUpdate(InstallState)`, Erasure-Konflikt). Stattdessen wird der Status bei jedem `OnResume` neu abgefragt — für den Zweck (Nutzer von alten, potenziell verbuggten Versionen wegbekommen) ausreichend.

**Immediate Flow** (blockierender Vollbild-Update-Zwang) bewusst nicht eingebaut — Entscheidung war Flexible, um Nutzer nicht zu bevormunden. Bei Bedarf für kritische Releases (z. B. ANR-Hotfixes) könnte das später ergänzt werden (Play-Console-Update-Priorität auswerten, dann `AppUpdateType.Immediate` statt `Flexible`).
