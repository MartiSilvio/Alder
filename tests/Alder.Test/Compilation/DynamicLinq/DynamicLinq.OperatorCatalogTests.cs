using System.Collections;
using System.Reflection;
using Alder.Compiled.DynamicLinq;

namespace Alder.Test.Compilation.DynamicLinq;

public partial class DynamicLinqTests
{
    [TestFixture]
    [NonParallelizable]
    public class OperatorCatalog : CompilerFixtureBase
    {
        [Test]
        public void Catalog_ExtensionSurface_MatchesImplementedOverloads()
        {
            var methods = typeof(Alder.Compiled.AlderLinqExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (var descriptor in DynamicLinqOperatorCatalog.Operators)
            {
                if (string.IsNullOrEmpty(descriptor.ExtensionName))
                    continue;

                var dynamicName = descriptor.ExtensionName + "Dynamic";
                var overloads = methods.Where(static method => method.Name.EndsWith("Dynamic", StringComparison.Ordinal))
                    .Where(method => method.Name == dynamicName)
                    .ToList();

                Assert.That(overloads, Is.Not.Empty, $"No overloads found for {dynamicName}.");

                if (descriptor.RequireEnumerableSource)
                {
                    Assert.That(
                        overloads.Any(method =>
                        {
                            if (method.GetParameters().Length == 0)
                                return false;

                            var sourceType = method.GetParameters()[0].ParameterType;
                            return sourceType == typeof(IEnumerable)
                                || (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                        }),
                        Is.True,
                        $"Expected IEnumerable source overload for {dynamicName}.");
                }

                if (descriptor.RequireQueryableSource)
                {
                    Assert.That(
                        overloads.Any(method =>
                        {
                            if (method.GetParameters().Length == 0)
                                return false;

                            var sourceType = method.GetParameters()[0].ParameterType;
                            return sourceType == typeof(IQueryable)
                                || (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(IQueryable<>));
                        }),
                        Is.True,
                        $"Expected IQueryable source overload for {dynamicName}.");
                }

                if (descriptor.RequireAsyncSource)
                {
                    Assert.That(
                        overloads.Any(method =>
                            method.GetParameters().Length > 0 &&
                            method.GetParameters()[0].ParameterType.IsGenericType &&
                            method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)),
                        Is.True,
                        $"Expected IAsyncEnumerable source overload for {dynamicName}.");
                }

                if (descriptor.RequireUntypedSequenceResult)
                {
                    Assert.That(
                        overloads.Any(method =>
                            method.ReturnType == typeof(IEnumerable) || method.ReturnType == typeof(IQueryable)),
                        Is.True,
                        $"Expected non-generic sequence result overload for {dynamicName}.");
                }

                if (descriptor.RequireUntypedScalarResult)
                {
                    Assert.That(
                        overloads.Any(static method => method.ReturnType == typeof(object)),
                        Is.True,
                        $"Expected object-returning overload for {dynamicName}.");
                }
            }
        }

        [Test]
        public void Catalog_DispatcherSurface_ResolvesMethodCache()
        {
            var operators = DynamicLinqOperatorCatalog.Operators.ToArray();
            var expectedKinds = Enum.GetValues<DynamicQueryOperatorKind>();
            var catalogKinds = operators
                .Where(static descriptor => descriptor.DispatcherOperatorKind is not null)
                .Select(static descriptor => descriptor.DispatcherOperatorKind!.Value)
                .Distinct()
                .ToArray();

            Assert.That(catalogKinds, Is.EquivalentTo(expectedKinds));

            foreach (var descriptor in operators)
            {
                if (descriptor.DispatcherOperatorKind is null)
                    continue;

                var probeType = DynamicLinqOperatorCatalog.ResolveProbeType(descriptor.DispatcherProbeType);
                Assert.DoesNotThrow(() => DynamicQueryMethodCache.GetMethod(
                    DynamicQueryProviderKind.Enumerable,
                    descriptor.DispatcherOperatorKind.Value,
                    probeType));
                Assert.DoesNotThrow(() => DynamicQueryMethodCache.GetMethod(
                    DynamicQueryProviderKind.Queryable,
                    descriptor.DispatcherOperatorKind.Value,
                    probeType));
            }
        }

        [Test]
        public void Catalog_DispatcherBackedWrappers_HaveExpectedSignatures()
        {
            var methods = typeof(Alder.Compiled.AlderLinqExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static);

            Assert.That(
                methods.Any(method =>
                    method.Name == "JoinDynamic" &&
                    method.IsGenericMethodDefinition &&
                    method.GetGenericArguments().Length == 2 &&
                    method.ReturnType == typeof(IEnumerable)),
                Is.True,
                "Expected generated non-generic IEnumerable JoinDynamic<TOuter, TInner> wrapper.");

            Assert.That(
                methods.Any(method =>
                    method.Name == "GroupJoinDynamic" &&
                    method.IsGenericMethodDefinition &&
                    method.GetGenericArguments().Length == 2 &&
                    method.ReturnType == typeof(IEnumerable)),
                Is.True,
                "Expected generated non-generic IEnumerable GroupJoinDynamic<TOuter, TInner> wrapper.");

            Assert.That(
                methods.Any(method =>
                    method.Name == "SumDynamic" &&
                    method.IsGenericMethodDefinition &&
                    method.GetGenericArguments().Length == 2 &&
                    method.ReturnType.IsGenericParameter &&
                    method.ReturnType.Name == "TResult"),
                Is.True,
                "Expected generated typed-result SumDynamic<T, TResult> wrapper.");

            Assert.That(
                methods.Any(method =>
                    method.Name == "AverageDynamic" &&
                    method.IsGenericMethodDefinition &&
                    method.GetGenericArguments().Length == 2 &&
                    method.ReturnType.IsGenericParameter &&
                    method.ReturnType.Name == "TResult"),
                Is.True,
                "Expected generated typed-result AverageDynamic<T, TResult> wrapper.");
        }

        [Test]
        public void Catalog_DispatcherBackedJoinWrappers_UseOuterSourceTypeParameter()
        {
            var methods = typeof(Alder.Compiled.AlderLinqExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static);

            Assert.That(HasOuterSourceWrapper(methods, "JoinDynamic", typeof(IEnumerable<>)), Is.True);
            Assert.That(HasOuterSourceWrapper(methods, "JoinDynamic", typeof(IQueryable<>)), Is.True);
            Assert.That(HasOuterSourceWrapper(methods, "GroupJoinDynamic", typeof(IEnumerable<>)), Is.True);
            Assert.That(HasOuterSourceWrapper(methods, "GroupJoinDynamic", typeof(IQueryable<>)), Is.True);
        }

        private static bool HasOuterSourceWrapper(
            IEnumerable<MethodInfo> methods,
            string name,
            Type sourceGenericDefinition)
        {
            return methods.Any(method =>
            {
                if (method.Name != name ||
                    !method.IsGenericMethodDefinition ||
                    method.GetGenericArguments().Length != 2)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 0 ||
                    !parameters[0].ParameterType.IsGenericType ||
                    parameters[0].ParameterType.GetGenericTypeDefinition() != sourceGenericDefinition)
                {
                    return false;
                }

                return parameters[0].ParameterType.GetGenericArguments()[0].Name == "TOuter";
            });
        }
    }
}
