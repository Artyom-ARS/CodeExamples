using System;
using fxcore2;

namespace FxConnect.Listeners
{
    public class MarketUpdateListener
    {
        public event EventHandler<TablesUpdatesEventArgs> UpdatePrice;

        public void OnUpdate(object sender, TablesUpdatesEventArgs e)
        {
            UpdatePrice?.BeginInvoke(sender, e, null, null);
        }
    }
}
