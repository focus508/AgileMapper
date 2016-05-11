﻿namespace AgileObjects.AgileMapper.Api.Configuration
{
    using ObjectPopulation;

    public class PostEventConfigStartingPoint
    {
        private readonly MapperContext _mapperContext;

        internal PostEventConfigStartingPoint(MapperContext mapperContext)
        {
            _mapperContext = mapperContext;
        }

        public InstanceCreationCallbackSpecifier<object, object, object> CreatingInstances
            => CreateCallbackSpecifier<object>();

        public InstanceCreationCallbackSpecifier<object, object, TInstance> CreatingInstancesOf<TInstance>() where TInstance : class
            => CreateCallbackSpecifier<TInstance>();

        private InstanceCreationCallbackSpecifier<object, object, TInstance> CreateCallbackSpecifier<TInstance>()
            => new InstanceCreationCallbackSpecifier<object, object, TInstance>(CallbackPosition.After, _mapperContext);
    }
}