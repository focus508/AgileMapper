namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;
    using Members;

    internal class ObjectMappingContext<TRuntimeSource, TRuntimeTarget> : IObjectMappingContext
    {
        #region Cached Items

        private static readonly ParameterExpression _parameter =
            Parameters.Create<ObjectMappingContext<TRuntimeSource, TRuntimeTarget>>("omc");

        // ReSharper disable StaticMemberInGenericType
        private static readonly Expression _sourceObjectProperty = Expression.Property(_parameter, "Source");

        private static readonly Expression _existingObjectProperty = Expression.Property(_parameter, "Existing");

        private static readonly Expression _enumerableIndexProperty = Expression.Property(_parameter, "EnumerableIndex");

        private static readonly ParameterExpression _targetVariable =
            Expression.Variable(typeof(TRuntimeTarget).GetTargetVariableType(), "target");

        private static readonly NestedAccessFinder _nestedAccessFinder = new NestedAccessFinder(_sourceObjectProperty);

        private static readonly Expression _mappingContextProperty = Expression.Property(_parameter, "MappingContext");

        private static readonly MethodCallExpression _tryGetCall = Expression.Call(
            _mappingContextProperty,
                _mappingContextProperty.Type
                    .GetMethod("TryGet", Constants.PublicInstance)
                    .MakeGenericMethod(_sourceObjectProperty.Type, _targetVariable.Type),
                _sourceObjectProperty,
                _targetVariable);

        private static readonly MethodCallExpression _createCall = Expression.Call(
            _parameter,
            _parameter.Type.GetMethod("Create", Constants.PublicInstance));

        private static readonly MethodCallExpression _registrationCall = Expression.Call(
            _mappingContextProperty,
                _mappingContextProperty.Type
                    .GetMethod("Register", Constants.PublicInstance)
                    .MakeGenericMethod(_sourceObjectProperty.Type, _targetVariable.Type),
                _sourceObjectProperty,
                _targetVariable);

        private static readonly MethodInfo _mapComplexTypeMethod =
            _parameter.Type
                .GetMethods(Constants.PublicInstance)
                .First(m => m.Name == "Map" && m.GetParameters().Length == 1);

        private static readonly MethodInfo _mapEnumerableMethod =
            _parameter.Type
                .GetMethods(Constants.PublicInstance)
                .First(m => m.Name == "Map" && m.GetParameters().Length == 2);

        private static readonly MethodInfo _mapEnumerableElementMethod =
            _parameter.Type
                .GetMethods(Constants.PublicInstance)
                .First(m => m.Name == "Map" && m.GetParameters().Length == 3);
        // ReSharper restore StaticMemberInGenericType

        #endregion

        private readonly QualifiedMember _sourceMember;
        private readonly QualifiedMember _targetMember;
        private readonly IObjectMappingContext _parent;
        private readonly int _sourceObjectDepth;

        public ObjectMappingContext(
            QualifiedMember sourceMember,
            QualifiedMember targetMember,
            TRuntimeSource source,
            TRuntimeTarget existing,
            int? enumerableIndex,
            MappingContext mappingContext)
        {
            _sourceMember = sourceMember;
            _targetMember = targetMember;
            EnumerableIndex = enumerableIndex;
            MappingContext = mappingContext;
            Source = source;
            Existing = existing;

            _parent = mappingContext.CurrentObjectMappingContext;
            _sourceObjectDepth = CalculateSourceObjectDepth();
        }

        private int CalculateSourceObjectDepth()
        {
            var parent = _parent;

            while (parent != null)
            {
                if (parent.HasSource(Source))
                {
                    parent = parent.Parent;
                    continue;
                }

                return parent.SourceObjectDepth + 1;
            }

            return 0;
        }

        public GlobalContext GlobalContext => MapperContext.GlobalContext;

        public MapperContext MapperContext => MappingContext.MapperContext;

        public MappingContext MappingContext { get; }

        public TRuntimeSource Source { get; }

        public TRuntimeTarget Existing { get; }

        public int? EnumerableIndex { get; }

        public TRuntimeTarget Create()
        {
            return MapperContext.ComplexTypeFactory.Create<TRuntimeTarget>();
        }

        public TMember Map<TMember>(Expression<Func<TRuntimeTarget, TMember>> complexChildMember)
            => MappingContext.MapChild(Source, Existing, complexChildMember);

        public TMember Map<TDeclaredSource, TMember>(
            TDeclaredSource sourceEnumerable,
            Expression<Func<TRuntimeTarget, TMember>> enumerableChildMember)
            => MappingContext.MapChild(sourceEnumerable, Existing, enumerableChildMember);

        public TTargetElement Map<TSourceElement, TTargetElement>(
            TSourceElement sourceElement,
            TTargetElement existingElement,
            int enumerableIndex)
        {
            return MappingContext.MapEnumerableElement(sourceElement, existingElement, enumerableIndex);
        }

        #region IMemberMappingContext Members

        IMemberMappingContext IMemberMappingContext.Parent => _parent;

        string IMemberMappingContext.RuleSetName => MappingContext.RuleSet.Name;

        Expression IMemberMappingContext.SourceObject => _sourceObjectProperty;

        Expression IMemberMappingContext.ExistingObject => _existingObjectProperty;

        Expression IMemberMappingContext.EnumerableIndex => _enumerableIndexProperty;

        ParameterExpression IMemberMappingContext.TargetVariable => _targetVariable;

        QualifiedMember IMemberMappingContext.TargetMember => _targetMember;

        NestedAccessFinder IMemberMappingContext.NestedAccessFinder => _nestedAccessFinder;

        #endregion

        #region IObjectMappingContext Members

        IObjectMappingContext IObjectMappingContext.Parent => _parent;

        ParameterExpression IObjectMappingContext.Parameter => _parameter;

        bool IObjectMappingContext.HasSource<TSource>(TSource source)
        {
            return ReferenceEquals(Source, source);
        }

        Type IObjectMappingContext.GetSourceMemberRuntimeType(QualifiedMember sourceMember)
        {
            if (sourceMember.Members.Count() == 1)
            {
                // The root member is guaranteed to be the runtime type:
                return typeof(TRuntimeSource);
            }

            var relativeMember = sourceMember.RelativeTo(_sourceObjectDepth);
            var memberAccess = relativeMember.GetAccess(_sourceObjectProperty);

            var getRuntimeTypeCall = Expression.Call(
                typeof(ObjectExtensions)
                    .GetMethod("GetRuntimeType", Constants.PublicStatic)
                    .MakeGenericMethod(sourceMember.Type),
                memberAccess);

            var getRuntimeTypeLambda = Expression
                .Lambda<Func<ObjectMappingContext<TRuntimeSource, TRuntimeTarget>, Type>>(
                    getRuntimeTypeCall,
                    _parameter);

            var getRuntimeTypeFunc = getRuntimeTypeLambda.Compile();

            return getRuntimeTypeFunc.Invoke(this);
        }

        int IObjectMappingContext.SourceObjectDepth => _sourceObjectDepth;

        QualifiedMember IObjectMappingContext.SourceMember => _sourceMember;

        MethodCallExpression IObjectMappingContext.GetTryGetCall() => _tryGetCall;

        MethodCallExpression IObjectMappingContext.GetCreateCall() => _createCall;

        MethodCallExpression IObjectMappingContext.GetObjectRegistrationCall() => _registrationCall;

        MethodCallExpression IObjectMappingContext.GetMapCall(Member complexTypeMember)
        {
            var mapCall = Expression.Call(
                _parameter,
                _mapComplexTypeMethod.MakeGenericMethod(complexTypeMember.Type),
                GetTargetMemberLambda(complexTypeMember));

            return mapCall;
        }

        private static LambdaExpression GetTargetMemberLambda(Member objectMember)
        {
            var targetObjectParameter = Parameters.Create<TRuntimeTarget>("t");
            var targetMemberAccess = objectMember.GetAccess(targetObjectParameter);

            var targetMemberLambda = Expression.Lambda(
                Expression.GetFuncType(targetObjectParameter.Type, objectMember.Type),
                targetMemberAccess,
                targetObjectParameter);

            return targetMemberLambda;
        }

        MethodCallExpression IObjectMappingContext.GetMapCall(Expression sourceEnumerable, Member enumerableMember)
        {
            var typedMapMethod = _mapEnumerableMethod
                .MakeGenericMethod(sourceEnumerable.Type, enumerableMember.Type);

            var mapCall = Expression.Call(
                _parameter,
                typedMapMethod,
                sourceEnumerable,
                GetTargetMemberLambda(enumerableMember));

            return mapCall;
        }

        MethodCallExpression IObjectMappingContext.GetMapCall(Expression sourceElement, Expression existingElement)
        {
            var typedMapMethod = _mapEnumerableElementMethod
                .MakeGenericMethod(sourceElement.Type, existingElement.Type);

            var mapCall = Expression.Call(
                _parameter,
                typedMapMethod,
                sourceElement,
                existingElement,
                Parameters.EnumerableIndex);

            return mapCall;
        }

        #endregion
    }
}