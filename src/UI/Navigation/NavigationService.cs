using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace EZPos.UI.Navigation
{
    public sealed class NavigationService
    {
        private readonly Dictionary<string, Func<UserControl>> routes;

        public NavigationService()
        {
            routes = new Dictionary<string, Func<UserControl>>(StringComparer.OrdinalIgnoreCase);
        }

        public void Register(string route, Func<UserControl> factory)
        {
            routes[route] = factory;
        }

        public bool TryCreatePage(string? route, out UserControl? page)
        {
            page = null;
            if (string.IsNullOrWhiteSpace(route))
            {
                return false;
            }

            if (!routes.TryGetValue(route, out var factory))
            {
                return false;
            }

            page = factory();
            return true;
        }
    }
}
