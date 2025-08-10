using CommunityToolkit.Maui.Views;

namespace RepeatList.Services
{
    public static class PopupHelper
    {
        public static async Task ClosePopupSafeAsync<TResult>(
                                        INavigation navigation,
                                        Popup popup,
                                        TResult result = default)
        {
            try
            {
                // Wenn Modal vorhanden: zuerst Modal entfernen (Popup kann blockiert sein).
                if (navigation.ModalStack.Any())
                {
                    await navigation.PopModalAsync();
                    // nachdem Modal entfernt ist, ist der Popup möglicherweise bereits weg
                    return;
                }

                // Wenn Popup ein generischer Popup<TResult> ist -> Close(result)
                if (popup is CommunityToolkit.Maui.Views.Popup<TResult> typedPopup)
                {
                    if (typedPopup.Handler != null)
                    {
                        // Close(result) liefert das Ergebnis an den ShowPopupAsync-Aufrufer
                        typedPopup.CloseAsync(result);
                    }
                }
                else
                {
                    // normaller non-generic Popup: nur CloseAsync() ohne Parameter
                    if (popup?.Handler != null)
                        await popup.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PopupHelper] Fehler beim Schließen: {ex}");
            }
        }

    }

}
