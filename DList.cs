using System.Collections;
using System.Collections.Generic;
using System;

public class DItem : IDItem
{
    public bool destroyed { get; set; }
}

public interface IDItem
{
    bool destroyed { get; set; }
}

/// <summary>
/// 提供延迟删除List元素的功能
/// </summary>
/// <typeparam name="T"></typeparam>
public class DList<T> : List<T> where T:IDItem
{
    bool m_RemoveDirty = false;
    bool m_AddDirty = false;
    List<T> delayAddList = new List<T>();

    public void DelayAdd(T item)
    {
        delayAddList.Add(item);
        m_AddDirty = true;
    }

    public void DelayRemove(T item)
    {
        item.destroyed = true;
        m_RemoveDirty = true;
    }

    public void DelayRemoveAt(int index)
    {
        Remove(this[index]);
    }

    public void ApplyDelayCommands()
    {
        if (this.m_RemoveDirty)
        {
            int count = this.Count;
            int i = 0, j = 0;
            while (i < count)
            {
                if (this[i].destroyed)
                {
                    j = i + 1;
                    while (j < count)
                    {
                        if (!this[j].destroyed)
                        {
                            this[i] = this[j];
                            i++;
                        }
                        j++;
                    }
                    this.RemoveRange(i, this.Count - i);
                    break;
                }
                i++;
            }
            this.m_RemoveDirty = false;
        }

        if (this.m_AddDirty)
        {
            this.AddRange(delayAddList);
            this.delayAddList.Clear();
            this.m_AddDirty = false;
        }
    }
}
