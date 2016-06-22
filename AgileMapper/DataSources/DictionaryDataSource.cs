﻿namespace AgileObjects.AgileMapper.DataSources
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;
    using Members;

    internal class DictionaryDataSource : DataSourceBase
    {
        #region Cached Items

        private static readonly MethodInfo _linqIntersectMethod = typeof(Enumerable)
            .GetMethods(Constants.PublicStatic)
            .First(m => m.Name == "Intersect" && m.GetParameters().Length == 3)
            .MakeGenericMethod(typeof(string));

        private static readonly MethodInfo _linqFirstOrDefaultMethod = typeof(Enumerable)
            .GetMethods(Constants.PublicStatic)
            .First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1)
            .MakeGenericMethod(typeof(string));

        #endregion

        public DictionaryDataSource(IMemberMappingContext context)
            : this(
                  context,
                  Expression.Variable(
                      context.SourceType.GetGenericArguments().Last(),
                      context.TargetMember.Name.ToCamelCase()))
        {
        }

        private DictionaryDataSource(IMemberMappingContext context, ParameterExpression variable)
            : base(context.SourceMember, Enumerable.Empty<Expression>(), new[] { variable }, GetValueParsing(variable, context))
        {
        }

        private static Expression GetValueParsing(Expression variable, IMemberMappingContext context)
        {
            var keysProperty = Expression.Property(context.SourceObject, "Keys");

            var potentialNamesArray = Expression.NewArrayInit(
                typeof(string),
                GetPotentialNames(context));

            var linqIntersect = Expression.Call(
                _linqIntersectMethod,
                keysProperty,
                potentialNamesArray,
                CaseInsensitiveStringComparer.InstanceMember);

            var intersectionFirstOrDefault = Expression.Call(_linqFirstOrDefaultMethod, linqIntersect);
            var emptyString = Expression.Field(null, typeof(string), "Empty");
            var matchingNameOrEmptyString = Expression.Coalesce(intersectionFirstOrDefault, emptyString);

            var tryGetValueCall = Expression.Call(
                context.SourceObject,
                context.SourceObject.Type.GetMethod("TryGetValue", Constants.PublicInstance),
                matchingNameOrEmptyString,
                variable);

            var defaultValue = context
                .MappingContext
                .RuleSet
                .FallbackDataSourceFactory
                .Create(context)
                .Value;

            var dictionaryValueOrDefault = Expression.Condition(tryGetValueCall, variable, defaultValue);

            return dictionaryValueOrDefault;
        }

        private static IEnumerable<Expression> GetPotentialNames(IMemberMappingContext context)
        {
            return context
                .TargetMember
                .MemberChain
                .Skip(1)
                .Select(context.MapperContext.NamingSettings.GetAlternateNamesFor)
                .CartesianProduct()
                .SelectMany(context.MapperContext.NamingSettings.GetJoinedNamesFor)
                .Select(Expression.Constant);
        }
    }
}