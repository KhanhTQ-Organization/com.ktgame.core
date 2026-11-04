using System;
using System.Collections.Generic;

namespace com.ktgame.core.di
{
    internal sealed class DisposableCollection : IDisposable
    {
        private readonly List<IDisposable> _items = new List<IDisposable>();

        public void Add(IDisposable disposable)
        {
            _items.Add(disposable);
        }

        public void TryAdd(object obj)
        {
            if (obj is IDisposable disposable)
            {
                _items.Add(disposable);
            }
        }

        public void Remove(IDisposable disposable)
        {
            var index = _items.LastIndexOf(disposable);
            if (index >= 0)
            {
                _items.RemoveAt(index);
            }
        }

        public void Dispose()
        {
            while (_items.Count > 0)
            {
                var lastIndex = _items.Count - 1;
                var disposable = _items[lastIndex];
                _items.RemoveAt(lastIndex);
                disposable.Dispose();
            }
        }
    }
}