using System;
using System.Threading;

namespace urbanx.Service
{
    public class CartService
    {
        private int _totalItems;
        public event EventHandler CartUpdated;

        public int TotalItems
        {
            get => _totalItems;
            set
            {
                if (_totalItems != value)
                {
                    _totalItems = value;
                    OnCartUpdated();
                }
            }
        }

        protected virtual void OnCartUpdated()
        {
            CartUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateTotalItems(int count)
        {
            TotalItems = count;
        }
    }
}