namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
#if NET_STANDARD
    using System.Reflection;
#endif

    internal class EnumerableTypeHelper
    {
        private readonly Type _enumerableType;
        private Type _listType;
        private Type _listInterfaceType;
        private Type _collectionType;
        private Type _enumerableInterfaceType;

        public EnumerableTypeHelper(Type enumerableType, Type elementType)
        {
            _enumerableType = enumerableType;
            ElementType = elementType;
        }

        public bool IsArray => _enumerableType.IsArray;

        public bool IsList => ListType.IsAssignableFrom(_enumerableType);

        public bool IsListInterface => ListInterfaceType.IsAssignableFrom(_enumerableType);

        public bool IsCollection => CollectionType.IsAssignableFrom(_enumerableType);

        public bool IsEnumerableInterface => _enumerableType == EnumerableInterfaceType;

        public Type ElementType { get; }

        public Type ListType => GetEnumerableType(ref _listType, typeof(List<>));

        public Type ListInterfaceType => GetEnumerableType(ref _listInterfaceType, typeof(IList<>));

        public Type CollectionType => GetEnumerableType(ref _collectionType, typeof(Collection<>));

        public Type EnumerableInterfaceType => GetEnumerableType(ref _enumerableInterfaceType, typeof(IEnumerable<>));

        private Type GetEnumerableType(ref Type typeField, Type openGenericEnumerableType)
            => typeField ?? (typeField = openGenericEnumerableType.MakeGenericType(ElementType));
    }
}