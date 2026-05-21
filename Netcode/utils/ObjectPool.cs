using System;
using System.Collections.Concurrent;

namespace Utils
{
    /// <summary>
    /// 通用对象池（线程安全、高性能、支持自定义创建/回收逻辑）
    /// </summary>
    /// <typeparam name="T">池内存储的对象类型</typeparam>
    public class ObjectPool<T>
    {
        #region 核心字段
        /// <summary>
        /// 线程安全的对象存储容器（核心），替代普通List实现高并发安全
        /// </summary>
        private readonly ConcurrentBag<T> _objectContainer;

        /// <summary>
        /// 对象创建工厂方法（无参）
        /// </summary>
        private readonly Func<T> _createFunc;

        /// <summary>
        /// 对象回收清理方法（归还对象时执行，可选）
        /// </summary>
        private readonly Action<T> _resetAction;

        /// <summary>
        /// 对象池最大容量（防止无限缓存导致内存溢出）
        /// </summary>
        public int MaxPoolSize { get; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化对象池（指定最大容量+对象创建方法）
        /// </summary>
        /// <param name="createFunc">对象创建逻辑（必须）</param>
        /// <param name="maxPoolSize">池最大容量，默认int.MaxValue</param>
        public ObjectPool(Func<T> createFunc, int maxPoolSize = int.MaxValue)
            : this(createFunc, null, maxPoolSize)
        {
        }

        /// <summary>
        /// 初始化对象池（完整版：创建方法+回收清理方法+最大容量）
        /// </summary>
        /// <param name="createFunc">对象创建逻辑（必须）</param>
        /// <param name="resetAction">对象回收时的清理逻辑（可选）</param>
        /// <param name="maxPoolSize">池最大容量，默认int.MaxValue</param>
        /// <exception cref="ArgumentNullException">创建方法不能为空</exception>
        /// <exception cref="ArgumentOutOfRangeException">最大容量必须大于0</exception>
        public ObjectPool(Func<T> createFunc, Action<T> resetAction, int maxPoolSize = int.MaxValue)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc), "对象创建工厂方法不能为空");
            _resetAction = resetAction;
            MaxPoolSize = maxPoolSize > 0 ? maxPoolSize : throw new ArgumentOutOfRangeException(nameof(maxPoolSize), "对象池最大容量必须大于0");
            _objectContainer = new ConcurrentBag<T>();
        }
        #endregion

        #region 核心方法
        /// <summary>
        /// 从对象池中获取一个对象（复用优先，无可用对象则创建）
        /// </summary>
        /// <returns>可用的对象实例</returns>
        public T Get()
        {
            // 线程安全：尝试从池中取出对象，成功则直接复用
            if (_objectContainer.TryTake(out var obj))
            {
                return obj;
            }
            // 池中无可用对象，执行创建逻辑生成新对象
            return _createFunc.Invoke();
        }

        /// <summary>
        /// 将对象归还到对象池（自动清理+容量校验）
        /// </summary>
        /// <param name="obj">需要归还的对象</param>
        /// <exception cref="ArgumentNullException">归还对象不能为空（值类型除外）</exception>
        public void Return(T obj)
        {
            // 值类型跳过空校验，引用类型校验非空
            if (obj == null && typeof(T).IsClass)
            {
                throw new ArgumentNullException(nameof(obj), "不能归还空对象到对象池");
            }

            // 校验池容量，未满则执行回收逻辑
            if (_objectContainer.Count < MaxPoolSize)
            {
                // 执行自定义清理方法，重置对象状态（关键：避免脏数据）
                _resetAction?.Invoke(obj);
                // 将清理后的对象放回池中
                _objectContainer.Add(obj);
            }
            // 容量已满则直接丢弃，避免内存溢出
        }

        /// <summary>
        /// 清空对象池，释放所有缓存的对象（主动回收内存）
        /// </summary>
        public void Clear()
        {
            _objectContainer.Clear();
        }

        /// <summary>
        /// 获取当前池内缓存的对象数量
        /// </summary>
        public int Count => _objectContainer.Count;
        #endregion
    }
}