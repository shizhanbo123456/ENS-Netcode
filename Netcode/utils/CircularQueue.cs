using System;

public class CircularQueue<T>
{
    private T[] _array;
    private int _capacity;
    private int _readIndex;
    // 写索引
    private int _writeIndex;
    public int Count { get; private set; }

    // 默认容量
    private const int DefaultCapacity = 300;
    // 扩容倍数
    private const int GrowFactor = 2;

    public int ReadIndex => _readIndex;
    public int WriteIndex => _writeIndex;
    // 获取当前队列总容量
    public int Capacity => _capacity;


    public CircularQueue() : this(DefaultCapacity) { }

    public CircularQueue(int capacity)
    {
        if (capacity <= 0)
        {
            throw new System.Exception("capacity must be greater than 0");
        }
        _capacity = capacity;
        _array = new T[_capacity];
        _readIndex = 0;
        _writeIndex = 0;
        Count = 0;
    }



    public void Write(T data)
    {
        if (Full())
        {
            GrowCapacity();
        }

        _array[_writeIndex] = data;
        _writeIndex = (_writeIndex + 1) % _capacity;
        Count++;
    }

    /// <summary>
    /// 读取数据，removeAfterRead=true 表示读取后移除元素
    /// </summary>
    public bool Read(out T data, bool removeAfterRead = true)
    {
        if (Empty())
        {
            data = default;
            return false;
        }

        data = _array[_readIndex];
        if (removeAfterRead)
        {
            // 清空元素
            _array[_readIndex] = default;
            // 读索引自增并取模
            _readIndex = (_readIndex + 1) % _capacity;
            Count--;
        }
        return true;
    }

    /// <summary>
    /// 移除队首元素
    /// </summary>
    public bool RemoveNext()
    {
        if (Empty()) return false;

        _array[_readIndex] = default;
        _readIndex = (_readIndex + 1) % _capacity;
        Count--;
        return true;
    }

    /// <summary>
    /// 获取队首元素（不移除）
    /// </summary>
    public bool Top(out T data)
    {
        if (Empty())
        {
            data = default;
            return false;
        }
        data = _array[_readIndex];
        return true;
    }

    public bool Empty() => Count == 0;
    public bool Full() => Count == _capacity;


    /// <summary>
    /// 扩容：创建新数组，复制原有数据，重置索引
    /// </summary>
    private void GrowCapacity()
    {
        // 计算新容量
        int newCapacity = _capacity * GrowFactor;
        T[] newArray = new T[newCapacity];

        // 复制环形队列数据到新数组（连续排列）
        if (Count > 0)
        {
            if (_readIndex < _writeIndex)
            {
                // 连续存储：直接复制
                System.Array.Copy(_array, _readIndex, newArray, 0, Count);
            }
            else
            {
                // 环形分段存储：分两段复制
                int firstPartLength = _capacity - _readIndex;
                System.Array.Copy(_array, _readIndex, newArray, 0, firstPartLength);
                System.Array.Copy(_array, 0, newArray, firstPartLength, _writeIndex);
            }
        }

        // 更新数组、容量、索引
        _array = newArray;
        _capacity = newCapacity;
        _readIndex = 0;
        _writeIndex = Count;
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new System.IndexOutOfRangeException();

            int actualIndex = (_readIndex + index) % _capacity;
            return _array[actualIndex];
        }
    }
}