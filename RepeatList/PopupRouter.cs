using CommunityToolkit.Maui.Views;

namespace RepeatList
{
    public static class PopupRouter
    {
        private static readonly Dictionary<string, Type> _popupRoutes = new();

        public static string RegisterPopup<T>() where T : Popup
        {
            string route = $"popup_{Guid.NewGuid()}";
            _popupRoutes[route] = typeof(T);
            Routing.RegisterRoute(route, typeof(T));
            return route;
        }

        public static void UnregisterPopup(string route)
        {
            if (_popupRoutes.ContainsKey(route))
            {
                Routing.UnRegisterRoute(route);
                _popupRoutes.Remove(route);
            }
        }
    }
}
