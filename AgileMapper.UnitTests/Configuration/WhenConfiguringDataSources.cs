﻿namespace AgileObjects.AgileMapper.UnitTests.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Shouldly;
    using TestClasses;
    using Xunit;

    public class WhenConfiguringDataSources
    {
        [Fact]
        public void ShouldApplyAConfiguredConstant()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<PublicProperty<string>>()
                    .To<PublicProperty<string>>()
                    .Map("Hello there!")
                    .To(x => x.Value);

                var source = new PublicProperty<string> { Value = "Goodbye!" };
                var result = mapper.Map(source).ToNew<PublicProperty<string>>();

                result.Value.ShouldBe("Hello there!");
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredConstantFromAllSourceTypes()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .To<PublicProperty<decimal>>()
                    .Map(decimal.MaxValue)
                    .To(x => x.Value);

                var mapNewResult1 = mapper.Map(new PublicField<decimal>()).ToNew<PublicProperty<decimal>>();
                var mapNewResult2 = mapper.Map(new Person()).ToNew<PublicProperty<decimal>>();
                var mapNewResult3 = mapper.Map(new PublicGetMethod<float>(1.0f)).ToNew<PublicProperty<decimal>>();

                mapNewResult1.Value.ShouldBe(decimal.MaxValue);
                mapNewResult2.Value.ShouldBe(decimal.MaxValue);
                mapNewResult3.Value.ShouldBe(decimal.MaxValue);
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredMember()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<Person>()
                    .To<PublicProperty<Guid>>()
                    .Map(x => x.Id)
                    .To(x => x.Value);

                var source = new Person { Id = Guid.NewGuid() };
                var result = mapper.Map(source).ToNew<PublicProperty<Guid>>();

                result.Value.ShouldBe(source.Id);
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredMemberFromAllSourceTypes()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .OnTo<Person>()
                    .Map(x => x.Id)
                    .To(x => x.Name);

                var targetPerson1 = new Person { Id = Guid.NewGuid() };
                var mapOnToResult1 = mapper.Map(new PublicField<decimal>()).OnTo(targetPerson1);

                var targetPerson2 = new Person { Id = Guid.NewGuid() };
                var mapOnToResult2 = mapper.Map(new Customer()).OnTo(targetPerson2);

                var targetPerson3 = new Person { Id = Guid.NewGuid() };
                var mapOnToResult3 = mapper.Map(new PublicProperty<DateTime>()).OnTo(targetPerson3);

                mapOnToResult1.Name.ShouldBe(targetPerson1.Id.ToString());
                mapOnToResult2.Name.ShouldBe(targetPerson2.Id.ToString());
                mapOnToResult3.Name.ShouldBe(targetPerson3.Id.ToString());
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredMemberInARootEnumerable()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<Person>()
                    .To<PublicField<string>>()
                    .Map(x => x.Name)
                    .To(x => x.Value);

                var source = new[] { new Person { Name = "Mr Thomas" } };
                var result = mapper.Map(source).ToNew<List<PublicField<string>>>();

                source.Select(p => p.Name).SequenceEqual(result.Select(r => r.Value)).ShouldBeTrue();
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredMemberInAMemberEnumerable()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<Customer>()
                    .To<PublicSetMethod<string>>()
                    .Map(x => x.Name)
                    .To<string>(x => x.SetValue);

                var source = new PublicProperty<Customer[]> { Value = new[] { new Customer { Name = "Mr Thomas" } } };
                var result = mapper.Map(source).ToNew<PublicField<IEnumerable<PublicSetMethod<string>>>>();

                source.Value.Select(p => p.Name).SequenceEqual(result.Value.Select(r => r.Value)).ShouldBeTrue();
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredMemberFromADerivedSourceType()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<Person>()
                    .To<PersonViewModel>()
                    .Map(x => x.Id)
                    .To(x => x.Name);

                var source = new Customer { Id = Guid.NewGuid(), Address = new Address() };
                var result = mapper.Map(source).ToNew<PersonViewModel>();

                result.Name.ShouldBe(source.Id.ToString());
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredExpression()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<PublicProperty<int>>()
                    .To<PublicField<long>>()
                    .Map(x => x.Value * 10)
                    .To(x => x.Value);

                var source = new PublicProperty<int> { Value = 123 };
                var result = mapper.Map(source).ToNew<PublicField<long>>();

                result.Value.ShouldBe(source.Value * 10);
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredExpressionWithMultipleSourceMembers()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<Person>()
                    .To<PersonViewModel>()
                    .Map(x => x.Address.Line1 + ", " + x.Address.Line2)
                    .To(x => x.AddressLine1);

                var source = new Person { Address = new Address { Line1 = "One", Line2 = "Two" } };
                var result = mapper.Map(source).ToNew<PersonViewModel>();

                result.AddressLine1.ShouldBe("One, Two");
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredExpressionToADerivedTargetType()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<PersonViewModel>()
                    .ToANew<Person>()
                    .Map(x => x.Name + "!")
                    .To(x => x.Name);

                var source = new PersonViewModel { Name = "Harry" };
                var result = mapper.Map(source).ToNew<Customer>();

                result.Name.ShouldBe(source.Name + "!");
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredFunction()
        {
            using (IMapper mapper = new Mapper())
            {
                Func<Person, string> combineAddressLine1 =
                    p => p.Name + ", " + p.Address.Line1;

                mapper.When.Mapping
                    .From<Person>()
                    .To<PersonViewModel>()
                    .Map(combineAddressLine1)
                    .To(x => x.AddressLine1);

                var source = new Person { Name = "Frank", Address = new Address { Line1 = "Over there" } };
                var result = mapper.Map(source).ToNew<PersonViewModel>();
                var expectedAddressLine1 = combineAddressLine1(source);

                result.AddressLine1.ShouldBe(expectedAddressLine1);
            }
        }

        [Fact]
        public void ShouldMapAConfiguredFunction()
        {
            using (IMapper mapper = new Mapper())
            {
                Func<Person, string> combineAddressLine1 =
                    p => p.Name + ", " + p.Address.Line1;

                mapper.When.Mapping
                    .From<Person>()
                    .To<PublicProperty<Func<Person, string>>>()
                    .MapFunc(combineAddressLine1)
                    .To(x => x.Value);

                var source = new Person { Name = "Frank", Address = new Address { Line1 = "Over there" } };
                var target = mapper.Map(source).Over(new PublicProperty<Func<Person, string>>());

                target.Value.ShouldBe(combineAddressLine1);
            }
        }

        [Fact]
        public void ShouldIgnoreAConfiguredMember()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<PersonViewModel>()
                    .ToANew<Person>()
                    .Ignore(x => x.Name);

                var source = new PersonViewModel { Name = "Jon" };
                var result = mapper.Map(source).ToNew<Person>();

                result.Name.ShouldBeNull();
            }
        }

        [Fact]
        public void ShouldIgnoreAConfiguredMemberInARootCollection()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<Person>()
                    .Over<Person>()
                    .Ignore(x => x.Address);

                var source = new[] { new Person { Name = "Jon", Address = new Address { Line1 = "Blah" } } };
                var target = new[] { new Person() };
                var result = mapper.Map(source).Over(target);

                result.Length.ShouldBe(1);
                result.First().Address.ShouldBeNull();
            }
        }

        [Fact]
        public void ShouldApplyAConfiguredExpressionUsingExtensionMethods()
        {
            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<Person>()
                    .To<Customer>()
                    .Map(x => (decimal)x.Name.First())
                    .To(x => x.Discount);

                var source = new Person { Name = "Bob" };
                var result = mapper.Map(source).ToNew<Customer>();

                result.Discount.ShouldBe(source.Name.First());
            }
        }

        [Fact]
        public void ShouldRestrictConfiguredConstantApplicationBySourceType()
        {
            const int CONFIGURED_VALUE = 12345;

            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<PublicField<int>>()
                    .To<PublicSetMethod<int>>()
                    .Map(CONFIGURED_VALUE)
                    .To<int>(x => x.SetValue);

                var matchingSource = new PublicField<int> { Value = 938726 };
                var matchingSourceResult = mapper.Map(matchingSource).ToNew<PublicSetMethod<int>>();

                var nonMatchingSource = new PublicProperty<int> { Value = matchingSource.Value };
                var nonMatchingSourceResult = mapper.Map(nonMatchingSource).ToNew<PublicSetMethod<int>>();

                matchingSourceResult.Value.ShouldBe(CONFIGURED_VALUE);
                nonMatchingSourceResult.Value.ShouldBe(nonMatchingSource.Value);
            }
        }

        [Fact]
        public void ShouldRestrictConfiguredConstantApplicationByTargetType()
        {
            const int CONFIGURED_VALUE = 98765;

            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<PublicField<int>>()
                    .To<PublicProperty<int>>()
                    .Map(CONFIGURED_VALUE)
                    .To(x => x.Value);

                var source = new PublicField<int> { Value = 938726 };
                var matchingTargetResult = mapper.Map(source).ToNew<PublicProperty<int>>();
                var nonMatchingTargetResult = mapper.Map(source).ToNew<PublicSetMethod<int>>();

                matchingTargetResult.Value.ShouldBe(CONFIGURED_VALUE);
                nonMatchingTargetResult.Value.ShouldBe(source.Value);
            }
        }

        [Fact]
        public void ShouldRestrictConfigurationApplicationByMappingMode()
        {
            const int CONFIGURED_VALUE = 9999;

            using (IMapper mapper = new Mapper())
            {
                mapper.When.Mapping
                    .From<PublicProperty<int>>()
                    .ToANew<PublicProperty<long>>()
                    .Map(CONFIGURED_VALUE)
                    .To(x => x.Value);

                var source = new PublicProperty<int> { Value = 64738 };
                var matchingModeResult = mapper.Map(source).ToNew<PublicProperty<long>>();

                var nonMatchingModeTarget = mapper.Map(source).Over(new PublicProperty<long>());

                matchingModeResult.Value.ShouldBe(CONFIGURED_VALUE);
                nonMatchingModeTarget.Value.ShouldBe(source.Value);
            }
        }
    }
}