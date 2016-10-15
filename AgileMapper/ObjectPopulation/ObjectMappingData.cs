namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System.Collections.Generic;
    using Members;

    internal class ObjectMappingData<TSource, TTarget> :
        BasicMappingData<TSource, TTarget>,
        IObjectMappingData
    {
        private readonly IMembersSource _membersSource;
        private readonly IObjectMappingData _parent;
        private readonly Dictionary<object, Dictionary<object, object>> _mappedObjectsByTypes;
        private readonly bool _isRoot;
        private ObjectMapperData _createdMapperData;

        public ObjectMappingData(
            TSource source,
            TTarget target,
            int? enumerableIndex,
            ObjectMapperKeyBase mapperKey,
            IMembersSource membersSource,
            IMappingContext mappingContext,
            IObjectMappingData parent = null)
            : base(
                  source,
                  target,
                  enumerableIndex,
                  mapperKey.MappingTypes.SourceType,
                  mapperKey.MappingTypes.TargetType,
                  mappingContext.RuleSet,
                  parent)
        {
            _membersSource = membersSource;
            MapperKey = mapperKey;
            mapperKey.MappingData = this;
            MappingContext = mappingContext;

            if (mapperKey.MappingTypes.IsEnumerable)
            {
                ElementMembersSource = new ElementMembersSource(this);
            }

            if (parent != null)
            {
                _parent = parent;
                return;
            }

            _mappedObjectsByTypes = new Dictionary<object, Dictionary<object, object>>();
            _isRoot = true;

            MapperContext.ObjectMapperFactory.CreateRoot(this);
        }

        public IMappingContext MappingContext { get; }

        public MapperContext MapperContext => MappingContext.MapperContext;

        public ObjectMapperKeyBase MapperKey { get; }

        public IObjectMapper Mapper { get; set; }

        public ElementMembersSource ElementMembersSource { get; }

        public TTarget CreatedObject { get; set; }

        #region IMappingData Members

        T IMappingData.GetSource<T>() => (T)(object)Source;

        T IMappingData.GetTarget<T>() => (T)(object)Target;

        public int? GetEnumerableIndex() => EnumerableIndex ?? Parent?.GetEnumerableIndex();

        IMappingData<TDataSource, TDataTarget> IMappingData.As<TDataSource, TDataTarget>()
            => (IMappingData<TDataSource, TDataTarget>)this;

        #endregion

        #region IObjectMappingData Members

        private ObjectMapperData _mapperData;

        public ObjectMapperData MapperData
        {
            get { return _mapperData ?? (_mapperData = CreateMapperData()); }
            set { _mapperData = value; }
        }

        private ObjectMapperData CreateMapperData()
        {
            lock (ObjectMapperData.Lock)
            {
                if (_createdMapperData != null)
                {
                    return _createdMapperData;
                }

                var sourceMember = _membersSource.GetSourceMember<TSource>().WithType(MapperKey.MappingTypes.SourceType);
                var targetMember = _membersSource.GetTargetMember<TTarget>().WithType(MapperKey.MappingTypes.TargetType);

                _createdMapperData = new ObjectMapperData(
                    MappingContext,
                    sourceMember,
                    targetMember,
                    _parent?.MapperData);
            }

            return _createdMapperData;
        }

        public IObjectMapper CreateMapper() => MapperKey.CreateMapper<TSource, TTarget>();

        private MemberMappingData<TSource, TTarget> _childMappingData;

        IMemberMappingData IObjectMappingData.GetChildMappingData(MemberMapperData childMapperData)
        {
            if (_childMappingData == null)
            {
                _childMappingData = new MemberMappingData<TSource, TTarget>(this);
            }

            _childMappingData.MapperData = childMapperData;

            return _childMappingData;
        }

        #endregion

        #region Map Methods

        public object MapStart() => Mapper.Map(this);

        public TDeclaredTarget Map<TDeclaredSource, TDeclaredTarget>(
            TDeclaredSource sourceValue,
            TDeclaredTarget targetValue,
            string targetMemberName,
            int dataSourceIndex)
        {
            return (TDeclaredTarget)Mapper.MapChild(
                sourceValue,
                targetValue,
                GetEnumerableIndex(),
                targetMemberName,
                dataSourceIndex,
                this);
        }

        public TTargetElement Map<TSourceElement, TTargetElement>(
            TSourceElement sourceElement,
            TTargetElement targetElement,
            int enumerableIndex)
        {
            return (TTargetElement)Mapper.MapElement(
                sourceElement,
                targetElement,
                enumerableIndex,
                this);
        }

        #endregion

        public bool TryGet<TKey, TComplex>(TKey key, out TComplex complexType)
        {
            if (!_isRoot)
            {
                return _parent.TryGet(key, out complexType);
            }

            if (key != null)
            {
                var typesKey = SourceAndTargetTypeKey<TKey, TComplex>.Instance;
                Dictionary<object, object> mappedTargetsBySource;

                if (_mappedObjectsByTypes.TryGetValue(typesKey, out mappedTargetsBySource))
                {
                    object mappedObject;

                    if (mappedTargetsBySource.TryGetValue(key, out mappedObject))
                    {
                        complexType = (TComplex)mappedObject;
                        return true;
                    }
                }
            }

            complexType = default(TComplex);
            return false;
        }

        public void Register<TKey, TComplex>(TKey key, TComplex complexType)
        {
            if (key == null)
            {
                return;
            }

            if (!_isRoot)
            {
                _parent.Register(key, complexType);
                return;
            }

            var typesKey = SourceAndTargetTypeKey<TKey, TComplex>.Instance;
            Dictionary<object, object> mappedTargetsBySource;

            if (_mappedObjectsByTypes.TryGetValue(typesKey, out mappedTargetsBySource))
            {
                mappedTargetsBySource[key] = complexType;
                return;
            }

            _mappedObjectsByTypes[typesKey] = new Dictionary<object, object> { [key] = complexType };
        }

        private class SourceAndTargetTypeKey<TKey, TComplex>
        {
            public static readonly object Instance = new SourceAndTargetTypeKey<TKey, TComplex>();
        }
    }
}