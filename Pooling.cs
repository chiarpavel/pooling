using System;
using System.Collections.Generic;

public class SimplePool<T> {
    protected List<T> available = new List<T>();
    protected List<T> inUse = new List<T>();

    /// <summary>
    /// Use this event to show hidden objects or enable objects that are disabled while not in use
    /// </summary>
    public event Action<T> OnGet;
    /// <summary>
    /// Use this event to hide shown objects or disable objects that are only enabled while in use
    /// </summary>
    public event Action<T> OnRelease;

    protected void InvokeOnGet(T item) {
        if (OnGet != null)
            OnGet.Invoke(item);
    }

    protected void InvokeOnRelease(T item) {
        if (OnRelease != null)
            OnRelease.Invoke(item);
    }

    public Pool(Func<int, T> CreateItem, int itemCount) {
        for (int i = 0; i < itemCount; i++) {
            T newItem = CreateItem(i);
            available.Add(newItem);
        }
    }

    public virtual T Get() {
        T item;

        if (available.Count == 0) {
            item = inUse[0];
            inUse.RemoveAt(0);
            inUse.Add(item);
        } else {
            item = available[0];
            available.Remove(item);
            inUse.Add(item);
        }
        InvokeOnGet(item);

        return item;
    }

    public virtual void Release(T item) {
        inUse.Remove(item);
        available.Add(item);
        InvokeOnRelease(item);
    }
}

// TODO: test and tweak FlexiblePool
// TODO: document SimplePool & FlexiblePool
public class FlexiblePool<T> : SimplePool<T> {
    private Func<int, T> CreateItem;
    private int initialItemCount;
    Dictionary<float, int> useHistory = new Dictionary<float, int>();
    int maxRecentUseCount = 0;
    float nextAdjustTime = 5f;

    public int maxCount = 256;
    public float sizeAdjustPeriod = 5f; // seconds
    public float margin = 1.2f; // amount of extra available elements to keep around

    public FlexiblePool(Func<int, T> CreateItem, int itemCount) : base(CreateItem, itemCount) {
        this.initialItemCount = itemCount;
        this.CreateItem = CreateItem;
    }

    public override T Get() {
        T item;

        if (available.Count == 0) {
            if (inUse.Count < maxCount) {
                item = CreateItem(inUse.Count);
            } else {
                item = inUse[0];
                inUse.RemoveAt(0);
            }
            inUse.Add(item);
        } else {
            item = available[0];
            available.RemoveAt(0);
            inUse.Add(item);
        }
        InvokeOnGet(item);

        return item;
    }

    public override void Release(T item) {
        inUse.Remove(item);
        available.Add(item);
        InvokeOnRelease(item);
    }

    /// <summary>
    /// Call this method in your update loop
    /// </summary>
    /// <param name="time">current time in seconds</param>
    public void MonitorSize(float time) {
        if (inUse.Count > maxRecentUseCount)
            maxRecentUseCount = inUse.Count;

        if (time > nextAdjustTime) {
            AdjustSize();
            maxRecentUseCount = inUse.Count;
            nextAdjustTime = time + sizeAdjustPeriod;
        }
    }

    private void AdjustSize() {
        if (available.Count < initialItemCount) return;

        int extraItems = available.Count - (int)(maxRecentUseCount * margin);
        if (extraItems > 0) {
            available.RemoveRange(0, extraItems);
        }
    }
}
