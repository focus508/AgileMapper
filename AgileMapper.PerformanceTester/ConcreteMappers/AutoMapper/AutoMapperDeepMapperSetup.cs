﻿namespace AgileObjects.AgileMapper.PerformanceTester.ConcreteMappers.AutoMapper
{
    using AbstractMappers;
    using global::AutoMapper;
    using TestClasses;

    internal class AutoMapperDeepMapperSetup : DeepMapperSetupBase
    {
        public override void Initialise()
        {
        }

        protected override void Reset()
        {
            Mapper.Reset();
        }

        protected override void SetupDeepMapper()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Address, Address>();
                cfg.CreateMap<Address, AddressDto>();
                cfg.CreateMap<Customer, CustomerDto>();
            });

            Mapper.Map<Customer, CustomerDto>(new Customer());
        }
    }
}