using Silk.NET.Vulkan;
using System;
using System.Threading;

namespace Ryujinx.Graphics.Vulkan
{
    class FenceHolder : IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;
        private Fence _fence;
        private int _referenceCount;
        private int _lock;
        private readonly bool _needsLock;
        private bool _disposed;

        public unsafe FenceHolder(Vk api, Device device, bool needsLock)
        {
            _api = api;
            _device = device;
            _needsLock = needsLock;

            var fenceCreateInfo = new FenceCreateInfo
            {
                SType = StructureType.FenceCreateInfo,
            };

            api.CreateFence(device, in fenceCreateInfo, null, out _fence).ThrowOnError();

            _referenceCount = 1;
        }

        public Fence GetUnsafe()
        {
            return _fence;
        }

        public bool TryGet(out Fence fence)
        {
            int lastValue;
            do
            {
                lastValue = _referenceCount;

                if (lastValue == 0)
                {
                    fence = default;
                    return false;
                }
            }
            while (Interlocked.CompareExchange(ref _referenceCount, lastValue + 1, lastValue) != lastValue);

            if (_needsLock)
            {
                AcquireLock();
            }

            fence = _fence;
            return true;
        }

        public Fence Get()
        {
            Interlocked.Increment(ref _referenceCount);
            return _fence;
        }

        public void PutLock()
        {
            Put();

            if (_needsLock)
            {
                ReleaseLock();
            }
        }

        public void Put()
        {
            if (Interlocked.Decrement(ref _referenceCount) == 0)
            {
                _api.DestroyFence(_device, _fence, Span<AllocationCallbacks>.Empty);
                _fence = default;
            }
        }

        private void AcquireLock()
        {
            while (!TryAcquireLock())
            {
                Thread.SpinWait(32);
            }
        }

        private bool TryAcquireLock()
        {
            return Interlocked.Exchange(ref _lock, 1) == 0;
        }

        private void ReleaseLock()
        {
            Interlocked.Exchange(ref _lock, 0);
        }

        public void Wait()
        {
            if (_needsLock)
            {
                AcquireLock();
            }

            FenceHelper.WaitAllIndefinitely(_api, _device, stackalloc Fence[] { _fence });

            if (_needsLock)
            {
                ReleaseLock();
            }
        }

        public bool IsSignaled()
        {
            if (_needsLock && !TryAcquireLock())
            {
                return false;
            }

            bool result = FenceHelper.AllSignaled(_api, _device, stackalloc Fence[] { _fence });

            if (_needsLock)
            {
                ReleaseLock();
            }

            return result;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Put();
                _disposed = true;
            }
        }
    }
}
