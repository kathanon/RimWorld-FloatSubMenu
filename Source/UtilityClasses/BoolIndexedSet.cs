using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FloatSubMenu.UtilityClasses {
    public class BoolIndexedSet<T> : HashSet<T> {
        public BoolIndexedSet() {}
        public BoolIndexedSet(int capacity) : base(capacity) {}
        public BoolIndexedSet(IEqualityComparer<T> comparer) : base(comparer) {}
        public BoolIndexedSet(IEnumerable<T> collection) : base(collection) {}
        public BoolIndexedSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) : base(collection, comparer) {}
        public BoolIndexedSet(int capacity, IEqualityComparer<T> comparer) : base(capacity, comparer) {}
        protected BoolIndexedSet(SerializationInfo info, StreamingContext context) : base(info, context) {}

        public bool this[T item] {
            get => Contains(item);
            set {
                if (value) Add(item);
                else       Remove(item);
            }
        }
    }
}
