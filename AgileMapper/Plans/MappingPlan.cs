﻿namespace AgileObjects.AgileMapper.Plans
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Members;
    using ObjectPopulation;
    using ReadableExpressions;
    using ReadableExpressions.Extensions;

    internal class MappingPlan<TSource, TTarget>
    {
        private readonly string _plan;

        public MappingPlan(MappingContext mappingContext)
        {
            var rootMappingData = mappingContext.CreateRootMapperCreationData(default(TSource), default(TTarget));

            var rootMapper = mappingContext
                .MapperContext
                .ObjectMapperFactory
                .CreateFor<TSource, TTarget>(rootMappingData);

            var planData = Expand(new MappingPlanData(
                mappingContext,
                rootMapper.MapperLambda,
                rootMappingData.MapperData));

            _plan = string.Join(
                Environment.NewLine + Environment.NewLine,
                planData.Distinct().Select(GetDescription));
        }

        private static IEnumerable<MappingPlanData> Expand(MappingPlanData planData)
        {
            yield return planData;

            var mapCalls = MapCallFinder.FindIn(planData.Lambda);

            foreach (var mapCall in mapCalls)
            {
                Func<MethodCallExpression, MappingPlanData, MappingPlanData> mappingLambdaFactory;

                if (IsObjectMemberMapping(mapCall))
                {
                    mappingLambdaFactory = ExpandObjectMapper;
                }
                else
                {
                    mappingLambdaFactory = ExpandElementMapper;
                }

                var nestedMappingFuncs = Expand(mappingLambdaFactory.Invoke(mapCall, planData));

                foreach (var nestedMappingFunc in nestedMappingFuncs)
                {
                    yield return nestedMappingFunc;
                }
            }
        }

        private static bool IsObjectMemberMapping(MethodCallExpression mapCall) => mapCall.Arguments.Count == 4;

        private static MappingPlanData ExpandObjectMapper(MethodCallExpression mapCall, MappingPlanData planData)
        {
            var targetMemberName = (string)((ConstantExpression)mapCall.Arguments[2]).Value;
            var dataSourceIndex = (int)((ConstantExpression)mapCall.Arguments[3]).Value;

            var typedExpandMethod = typeof(MappingPlan<TSource, TTarget>)
                .GetMethods(Constants.NonPublicStatic)
                .First(m => m.ContainsGenericParameters && (m.Name == "ExpandObjectMapper"))
                .MakeGenericMethod(
                    mapCall.Arguments[0].Type,
                    mapCall.Arguments[1].Type);

            var childMapperData = typedExpandMethod.Invoke(
                null,
                new object[] { targetMemberName, dataSourceIndex, planData });

            return (MappingPlanData)childMapperData;
        }

        // ReSharper disable once UnusedMember.Local
        private static MappingPlanData ExpandObjectMapper<TChildSource, TChildTarget>(
            string targetMemberName,
            int dataSourceIndex,
            MappingPlanData planData)
        {
            var instanceData = new MappingInstanceData<TChildSource, TChildTarget>(
                planData.MappingContext,
                default(TChildSource),
                default(TChildTarget));

            var childMapperCreationData = planData.MapperData.CreateChildMapperCreationData(
                instanceData,
                targetMemberName,
                dataSourceIndex);

            var targetType = childMapperCreationData.MapperData.TargetType;

            LambdaExpression mappingLambda;

            if (targetType == typeof(TChildTarget))
            {
                mappingLambda = GetMappingLambda<TChildSource, TChildTarget>(childMapperCreationData);
            }
            else
            {
                mappingLambda = (LambdaExpression)typeof(MappingPlan<TSource, TTarget>)
                    .GetMethod("GetMappingLambda", Constants.NonPublicStatic)
                    .MakeGenericMethod(childMapperCreationData.MapperData.SourceType, targetType)
                    .Invoke(null, new object[] { childMapperCreationData });
            }

            return new MappingPlanData(
                planData.MappingContext,
                mappingLambda,
                childMapperCreationData.MapperData);
        }

        private static LambdaExpression GetMappingLambda<TChildSource, TChildTarget>(IObjectMapperCreationData data)
        {
            var mapper = data
                .MapperData
                .MapperContext
                .ObjectMapperFactory
                .CreateFor<TChildSource, TChildTarget>(data);

            return mapper.MapperLambda;
        }

        private static MappingPlanData ExpandElementMapper(MethodCallExpression mapCall, MappingPlanData planData)
        {
            var typedExpandMethod = typeof(MappingPlan<TSource, TTarget>)
                .GetMethods(Constants.NonPublicStatic)
                .First(m => m.ContainsGenericParameters && (m.Name == "ExpandElementMapper"))
                .MakeGenericMethod(mapCall.Method.GetGenericArguments());

            var childMapperData = typedExpandMethod.Invoke(
                null,
                new object[] { planData });

            return (MappingPlanData)childMapperData;
        }

        // ReSharper disable once UnusedMember.Local
        private static MappingPlanData ExpandElementMapper<TSourceElement, TTargetElement>(MappingPlanData planData)
        {
            var elementInstanceData = new MappingInstanceData<TSourceElement, TTargetElement>(
                planData.MappingContext,
                default(TSourceElement),
                default(TTargetElement));

            var elementMapperCreationData = planData.MapperData.CreateElementMapperCreationData(elementInstanceData);

            var mappingLambda = GetMappingLambda<TSourceElement, TTargetElement>(elementMapperCreationData);

            return new MappingPlanData(
                planData.MappingContext,
                mappingLambda,
                elementMapperCreationData.MapperData);
        }

        private static string GetDescription(MappingPlanData mappingPlanData)
        {
            var mappingTypes = mappingPlanData.Lambda.Type.GetGenericArguments();
            var sourceType = mappingTypes.ElementAt(0).GetFriendlyName();
            var targetType = mappingTypes.ElementAt(1).GetFriendlyName();

            return $@"
// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
// Map {sourceType} -> {targetType}
// Rule Set: {mappingPlanData.MapperData.RuleSet.Name}
// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

{mappingPlanData.Lambda.ToReadableString()}".TrimStart();
        }

        public static implicit operator string(MappingPlan<TSource, TTarget> mappingPlan) => mappingPlan._plan;

        private class MapCallFinder : ExpressionVisitor
        {
            private readonly ParameterExpression _omcParameter;
            private readonly ICollection<MethodCallExpression> _mapCalls;

            private MapCallFinder(ParameterExpression omcParameter)
            {
                _omcParameter = omcParameter;
                _mapCalls = new List<MethodCallExpression>();
            }

            public static IEnumerable<MethodCallExpression> FindIn(LambdaExpression mappingLambda)
            {
                var finder = new MapCallFinder(mappingLambda.Parameters.First());

                finder.Visit(mappingLambda);

                return finder._mapCalls;
            }

            protected override Expression VisitMethodCall(MethodCallExpression methodCall)
            {
                if (IsMapCall(methodCall))
                {
                    _mapCalls.Add(methodCall);
                }

                return base.VisitMethodCall(methodCall);
            }

            private bool IsMapCall(MethodCallExpression methodCall)
                => (methodCall.Object == _omcParameter) && (methodCall.Method.Name == "Map");
        }
    }
}
