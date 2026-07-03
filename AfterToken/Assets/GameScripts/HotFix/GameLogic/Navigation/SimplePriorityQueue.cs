using System;
using System.Collections.Generic;

namespace GameLogic.Navigation
{
    /// <summary>
    /// 简单二叉堆优先队列（最小堆）。
    /// </summary>
    public class SimplePriorityQueue<T>
    {
        private readonly List<(T element, float priority)> _heap = new();

        public int Count => _heap.Count;

        public void Enqueue(T element, float priority)
        {
            _heap.Add((element, priority));
            HeapifyUp(_heap.Count - 1);
        }

        public T Dequeue()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("Priority queue is empty");

            T result = _heap[0].element;
            int lastIndex = _heap.Count - 1;
            _heap[0] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);

            if (_heap.Count > 0)
                HeapifyDown(0);

            return result;
        }

        public bool TryDequeue(out T element, out float priority)
        {
            if (_heap.Count == 0)
            {
                element = default;
                priority = 0;
                return false;
            }

            var top = _heap[0];
            element = top.element;
            priority = top.priority;
            Dequeue();
            return true;
        }

        public void Clear()
        {
            _heap.Clear();
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_heap[parent].priority <= _heap[index].priority)
                    break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int count = _heap.Count;
            while (true)
            {
                int left = index * 2 + 1;
                int right = index * 2 + 2;
                int smallest = index;

                if (left < count && _heap[left].priority < _heap[smallest].priority)
                    smallest = left;
                if (right < count && _heap[right].priority < _heap[smallest].priority)
                    smallest = right;

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var temp = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = temp;
        }
    }
}
