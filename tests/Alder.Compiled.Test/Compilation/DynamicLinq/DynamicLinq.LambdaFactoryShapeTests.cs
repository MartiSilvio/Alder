using System.Linq.Expressions;
using Alder.Compiled.DynamicLinq;
using Alder.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Alder.Test.Compilation.DynamicLinq;

public partial class DynamicLinqTests
{
    [TestFixture]
    [NonParallelizable]
    public class LambdaFactoryShape : CompilerFixtureBase
    {
        [Test]
        public void ParseLambda_ItTypeOverload_ReturnsPlanWithExpressionInterop()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var plan = engine.ParseLambda(
                typeof(Product),
                typeof(bool),
                "p => p.Price > @0",
                [new KeyValuePair<string, object?>("__p0", 50m)]);
            var lambda = plan.ToExpression<Func<Product, bool>>();

            Assert.That(lambda, Is.Not.Null);
            Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
            Assert.That(lambda.ReturnType, Is.EqualTo(typeof(bool)));

            var fn = lambda.Compile();
            Assert.That(fn(new Product("Test", 75m, "X", true)), Is.True);
        }

        [Test]
        public void ParseLambda_ParameterTypesOverload_SupportsBodyWithoutLambdaSyntax()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var plan = engine.ParseLambda(
                [typeof(Product), typeof(decimal)],
                ["p", "threshold"],
                typeof(bool),
                "p.Price > threshold");
            var lambda = plan.ToExpression<Func<Product, decimal, bool>>();

            Assert.That(lambda.Parameters, Has.Count.EqualTo(2));
            Assert.That(lambda.ReturnType, Is.EqualTo(typeof(bool)));

            var fn = lambda.Compile();
            Assert.That(fn(new Product("Test", 75m, "X", true), 50m), Is.True);
        }

        [Test]
        public void ParseLambda_ParameterExpressionOverload_BindsExplicitParameters()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var left = Expression.Parameter(typeof(int), "left");
            var right = Expression.Parameter(typeof(int), "right");

            var plan = engine.ParseLambda(
                [left, right],
                typeof(int),
                "left + right");
            var lambda = plan.ToExpression<Func<int, int, int>>();

            var fn = lambda.Compile();
            Assert.That(fn(20, 22), Is.EqualTo(42));
        }

        [Test]
        public void ParseSelector_ExplicitGenericStaticMethod_IsSupported()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());

            var lambda = engine.ParseSelector(
                typeof(Product),
                typeof(int),
                "Task.FromResult<int>((int)Price).Result")
                .ToExpression<Func<Product, int>>();

            var fn = lambda.Compile();
            Assert.That(fn(new Product("Test", 42.9m, "X", true)), Is.EqualTo(42));
        }

        [Test]
        public void ParseSelector_StaticTypePropertyChain_IsSupported()
        {
            using var engine = new AlderEngine(o =>
            {
                o.UseCompiler();
                o.IsCaseSensitive = false;
            });

            var lambda = engine.ParseSelector(
                typeof(Product),
                typeof(int),
                "datetime.minvalue.second")
                .ToExpression<Func<Product, int>>();

            var fn = lambda.Compile();
            Assert.That(fn(new Product("Test", 1m, "X", true)), Is.EqualTo(DateTime.MinValue.Second));
        }

        [Test]
        public void ParseSelector_EfProperty_GenericStaticMethod_IsSupported()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());

            var typed = engine.ParseSelector(
                typeof(Product),
                typeof(decimal),
                """EF.Property<decimal>(it, "Price")""")
                .ToExpression<Func<Product, decimal>>();

            Assert.That(typed.Body, Is.InstanceOf<MethodCallExpression>());

            var call = (MethodCallExpression)typed.Body;
            Assert.That(call.Method.DeclaringType, Is.EqualTo(typeof(EF)));
            Assert.That(call.Method.Name, Is.EqualTo(nameof(EF.Property)));
            Assert.That(call.Method.IsGenericMethod, Is.True);
            Assert.That(call.Method.GetGenericArguments()[0], Is.EqualTo(typeof(decimal)));
        }

        [Test]
        public void ParsePredicate_JObjectBodyOnlyMemberAccess_IsRejectedInExpressionTreeMode()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());

            var ex = Assert.Throws<AlderException>(() =>
                engine.ParsePredicate(typeof(JObject), """City == "Paris" """));

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
        }

        [Test]
        public void ParseSelector_ReflectionAssemblyAccess_IsRejected()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());

            var ex = Assert.Throws<AlderException>(() =>
                engine.ParseSelector(typeof(Product), typeof(object), "typeof(DateTime).Assembly"));

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0108));
        }
    }
}
