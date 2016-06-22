﻿namespace AgileObjects.AgileMapper.UnitTests
{
    using System.Collections.Generic;
    using Shouldly;
    using TestClasses;
    using Xunit;

    public class WhenMappingFromDictionaries
    {
        [Fact]
        public void ShouldPopulateASimpleTypeMember()
        {
            var source = new Dictionary<string, int> { ["Value"] = 123 };
            var result = Mapper.Map(source).ToANew<PublicField<int>>();

            result.ShouldNotBeNull();
            result.Value.ShouldBe(123);
        }

        [Fact]
        public void ShouldPopulateASimpleTypeMemberCaseInsensitively()
        {
            var source = new Dictionary<string, string> { ["value"] = "Hello" };
            var result = Mapper.Map(source).ToANew<PublicField<string>>();

            result.Value.ShouldBe("Hello");
        }

        [Fact]
        public void ShouldPopulateASimpleTypeSetMethod()
        {
            var source = new Dictionary<string, string> { ["Value"] = "Goodbye" };
            var result = Mapper.Map(source).ToANew<PublicSetMethod<string>>();

            result.Value.ShouldBe("Goodbye");
        }

        [Fact]
        public void ShouldPopulateAComplexTypeSimpleTypeMemberByFlattenedName()
        {
            var source = new Dictionary<string, string> { ["ValueValue"] = "Over here!" };
            var result = Mapper.Map(source).ToANew<PublicField<PublicField<string>>>();

            result.Value.Value.ShouldBe("Over here!");
        }

        [Fact]
        public void ShouldPopulateAComplexTypeSimpleTypeMemberByDottedName()
        {
            var source = new Dictionary<string, string> { ["Value.Value"] = "Over there!" };
            var result = Mapper.Map(source).ToANew<PublicProperty<PublicProperty<string>>>();

            result.Value.Value.ShouldBe("Over there!");
        }
    }
}
